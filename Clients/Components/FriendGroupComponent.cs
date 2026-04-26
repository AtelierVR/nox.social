using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Social.Clients.Pages;
using UnityEngine;
using Nox.CCK.UI;

namespace Nox.Social.Clients.Components {
	public class FriendGroupComponent : MonoBehaviour {
		public string key;

		public TextLanguage Text;
		public RectTransform Content;

		public static UniTask<GameObject> BoxPrefab
			=> Client.GetAssetAsync<GameObject>("ui:prefabs/box.prefab");
		public static UniTask<GameObject> ListPrefab
			=> Client.GetAssetAsync<GameObject>("ui:prefabs/list.prefab");
		public static UniTask<GameObject> GridPrefab
			=> Client.GetAssetAsync<GameObject>("ui:prefabs/grid_group.prefab");

		public List<FriendElementComponent> Elements = new();

		public async UniTask UpdateContent(FriendsPage.Group group, GameObject itemPrefab = null, GameObject elementPrefab = null) {
			itemPrefab    ??= await FriendElementComponent.ItemPrefab;
			elementPrefab ??= await FriendElementComponent.ElementPrefab;

			if (!string.IsNullOrEmpty(group.Title))
				Text.UpdateText("value", new[] { group.Title });
			else if (group.Key == "all")
				Text.UpdateText("friends.all");
			else
				Text.UpdateText("friends.group", new[] { group.Key });

			// Remove extra groups
			for (var i = Elements.Count - 1; i >= 0; i--) {
				// Check if not present in groups
				var element = Elements[i];
				if (group.Elements.Any(e => e.Identifier.Equals(element.Identifier)))
					continue;
				// Not present, remove
				element.Destroy();
				Elements.RemoveAt(i);
			}

			// Create/Update groups
			foreach (var user in group.Elements) {
				var component = Elements.FirstOrDefault(e => e.Identifier.Equals(user.Identifier));
				if (!component) {
					component = await FriendElementComponent.Create(user.Identifier, Content, itemPrefab, elementPrefab);
					Elements.Add(component);
				}
				await component.UpdateContent(user);
			}
		}
		
		public static async UniTask<FriendGroupComponent> Create(string groupKey, RectTransform parent, GameObject boxPrefab = null, GameObject gridPrefab = null) {
				boxPrefab  ??= await BoxPrefab;
				gridPrefab ??= await GridPrefab;
				var go        = await boxPrefab.InstantiateAsync(parent);
				var component = go.AddComponent<FriendGroupComponent>();
				component.key  = groupKey;
				component.Text = Reference.GetComponent<TextLanguage>("text", go);
				var content = Reference.GetComponent<RectTransform>("content", go);
				var group   = await gridPrefab.InstantiateAsync(content);
				Reference.GetComponent<WidgetGrid>("grid", group)
					.dimensions = new Vector2Int(9, 0);
				component.Content = Reference.GetComponent<RectTransform>("content", group);
				return component;
			}
		}
	}