using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Social.Clients.Pages;
using UnityEngine;
using UnityEngine.UI;

namespace Nox.Social.Clients.Components {
	public class FriendsComponent : MonoBehaviour {
		public FriendsPage Page;

		private RectTransform content;
		private List<FriendGroupComponent> _groups = new();

		private GameObject    _statusContainer;
		private TextLanguage  _statusText;

		public static (GameObject, FriendsComponent) Create(FriendsPage page, RectTransform parent) {
			var iconAsset      = Client.GetAsset<GameObject>("ui:prefabs/header_icon.prefab");
			var labelAsset     = Client.GetAsset<GameObject>("ui:prefabs/header_label.prefab");
			var withTitleAsset = Client.GetAsset<GameObject>("ui:prefabs/with_title.prefab");
			var listAsset      = Client.GetAsset<GameObject>("ui:prefabs/list.prefab");
			var scrollAsset    = Client.GetAsset<GameObject>("ui:prefabs/scroll.prefab");
			var dropdownAsset  = Client.GetAsset<GameObject>("ui:prefabs/header_dropdown.prefab");
			var buttonAsset    = Client.GetAsset<GameObject>("ui:prefabs/header_button.prefab");

			var content = Client.GetAsset<GameObject>("ui:prefabs/split.prefab").Instantiate(parent);

			var component = content.AddComponent<FriendsComponent>();
			component.Page = page;
			content.name   = $"[{page.GetKey()}_{content.GetId()}]";

			var splitContent = Reference.GetComponent<RectTransform>("content", content);

			// container
			var container = Instantiate(Client.GetAsset<GameObject>("ui:prefabs/container_full.prefab"), splitContent);
			var withTitle = Instantiate(withTitleAsset, Reference.GetComponent<RectTransform>("content", container));

			var header = Reference.GetReference("header", withTitle);
			Reference.GetComponent<Image>("image", iconAsset.Instantiate(Reference.GetComponent<RectTransform>("before", header)))
				.sprite = Client.GetAsset<Sprite>("ui:icons/friend.png");
			Reference.GetComponent<TextLanguage>("text", labelAsset.Instantiate(Reference.GetComponent<RectTransform>("content", header)))
				.UpdateText("friends.title");

			var after = Reference.GetComponent<RectTransform>("after", header);

			#region Request Button

			var button = buttonAsset.Instantiate(after);
			Reference.GetComponent<Image>("image", button)
				.sprite = Client.GetAsset<Sprite>("ui:icons/person_add.png");
			Reference.GetComponent<Button>("button", button)
				.onClick.AddListener(component.OnRequestClicked);

			#endregion

			#region Refresh Button

			button = buttonAsset.Instantiate(after);
			Reference.GetComponent<Image>("image", button)
				.sprite = Client.GetAsset<Sprite>("ui:icons/refresh.png");
			Reference.GetComponent<Button>("button", button)
				.onClick.AddListener(component.OnRefreshClicked);

			#endregion

			var contentDash = Reference.GetComponent<RectTransform>("content", withTitle);

			// status box (loading / empty / error) shown above the list
			component._statusContainer = Client.GetAsset<GameObject>("ui:prefabs/infobox.prefab").Instantiate(contentDash);
			component._statusText      = Reference.GetComponent<TextLanguage>("text", component._statusContainer);
			component._statusContainer.SetActive(false);

			// setup scroll + list
			var scroll = scrollAsset.Instantiate(contentDash);
			var list   = listAsset.Instantiate(Reference.GetComponent<RectTransform>("content", scroll));
			component.content = Reference.GetComponent<RectTransform>("content", list);


			return (content, component);
		}
		private void OnRefreshClicked()
			=> Page.OnRefresh();

		private void OnRequestClicked() {
			throw new System.NotImplementedException();
		}

		/// <summary>Shows a status message (loading / empty / error) above the list.</summary>
		public void SetStatus(string key, string[] args = null) {
			_statusText.UpdateText(key, args);
			_statusContainer.SetActive(true);
			UpdateLayout.UpdateImmediate(_statusContainer);
		}

		/// <summary>Hides the status message.</summary>
		public void ClearStatus() {
			_statusContainer.SetActive(false);
		}

		private CancellationTokenSource _cts;

		private void OnDestroy() {
			_cts?.Cancel();
			_cts?.Dispose();
		}

		async internal UniTask UpdateContent() {
			var box  = await FriendGroupComponent.BoxPrefab;
			var grid = await FriendGroupComponent.GridPrefab;

			_cts?.Cancel();
			_cts?.Dispose();

			_cts = new CancellationTokenSource();

			var groups = Page.Groups ?? Array.Empty<FriendsPage.Group>();

			// Remove extra groups
			for (var i = _groups.Count - 1; i >= 0; i--) {
				var group = _groups[i];
				if (groups.Any(g => g.Key == group.key))
					continue;
				group.Destroy();
				_groups.RemoveAt(i);
			}

			// Step 1: create/update the group containers and their headers, then show them
			// immediately (before any card is loaded).
			foreach (var group in groups) {
				var component = _groups.FirstOrDefault(g => g.key == group.Key);
				if (!component) {
					component = await FriendGroupComponent.Create(group.Key, content, box, grid);
					_groups.Add(component);
					UpdateLayout.UpdateImmediate(content);
				}
				component.UpdateHeader(group);
			}

			UpdateLayout.UpdateImmediate(content);

			// The headers are visible now: hide the loading indicator before loading the cards.
			if (groups.Length > 0)
				ClearStatus();

			// Step 2: load the cards afterwards, group by group.
			var item    = await FriendElementComponent.ItemPrefab;
			var element = await FriendElementComponent.ElementPrefab;
			foreach (var group in groups) {
				var component = _groups.FirstOrDefault(g => g.key == group.Key);
				if (!component)
					continue;
				await component.UpdateContent(group, item, element);
				UpdateLayout.UpdateImmediate(content);
			}
		}
	}
}