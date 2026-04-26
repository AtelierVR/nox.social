using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Social.Clients.Pages;
using Nox.Users;
using UnityEngine;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Social.Clients.Components {
	public class FriendsComponent : MonoBehaviour {
		public FriendsPage Page;

		private RectTransform content;
		private List<FriendGroupComponent> _groups = new();

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
			content.name   = $"[{page.GetKey()}_{content.GetEntityId().GetHashCode()}]";

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

		private CancellationTokenSource _cts;

		private void OnDestroy() {
			_cts?.Cancel();
			_cts?.Dispose();
		}

		async internal UniTask UpdateContent() {
			var box  = await FriendGroupComponent.BoxPrefab;
			var grid = await FriendGroupComponent.GridPrefab;
			var item = await FriendElementComponent.ItemPrefab;
			var element = await FriendElementComponent.ElementPrefab;
			
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

			// Create/Update groups
			foreach (var group in groups) {
				var component = _groups.FirstOrDefault(g => g.key == group.Key);
				if (!component) {
					component = await FriendGroupComponent.Create(group.Key, content, box, grid);
					_groups.Add(component);
				}
				await component.UpdateContent(group, item, element);
			}

			UpdateLayout.UpdateImmediate(content);
		}
	}
}