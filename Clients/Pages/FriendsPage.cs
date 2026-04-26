using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Social.Clients.Components;
using Nox.UI;
using Nox.Users;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Social.Clients.Pages {
	public class FriendsPage : IPage {
		public static string GetStaticKey()
			=> "friends";

		public string GetKey()
			=> GetStaticKey();

		private int _menuId;
		private object[] _context;
		private GameObject _content;
		private FriendsComponent _component;

		public class Group {
			public string Key;
			public string Title;
			public IUser[] Elements;
		}

		public Group[] Groups = Array.Empty<Group>();

		public static IPage Create(IMenu menu, object[] context)
			=> new FriendsPage {
				_menuId  = menu.Id,
				_context = context
			};

		public object[] GetContext()
			=> _context;

		public IMenu GetMenu()
			=> Client.UiAPI?.Get<IMenu>(_menuId);

		public GameObject GetContent(RectTransform parent) {
			if (_content)
				return _content;
			(_content, _component) = FriendsComponent.Create(this, parent);
			return _content;
		}

		public void OnOpen(IPage lastPage)
			=> Refresh(false).Forget();

		public void OnDisplay(IPage lastPage)
			=> Refresh(true).Forget();

		public void OnRefresh()
			=> Refresh(true).Forget();

		public void OnRemove() {
			_component = null;
			_content   = null;
		}

		private async UniTask Refresh(bool updateComponent = false) {
			List<IUser> users = new();

			var search = await Client.UserAPI.FetchFriends();
			if (search == null) {
				Logger.LogWarning("No users found", tag: nameof(FriendsPage));
				goto ended;
			}

			users.AddRange(search.Items);
			do {
				search = await search.Next();
				if (search == null) {
					Logger.LogWarning("Failed to fetch more friends", tag: nameof(FriendsPage));
					break;
				}
				users.AddRange(search.Items);
			} while (search.HasNext());

			// TODO: Implement multiple favorite groups instead of just one
			List<(string, string, Identifier[])> tables = new();

			var table = await Client.UserAPI.GetFavorites();
			if (table != null)
				tables.Add((table.Label, table.Key, table.Values.ToArray()));

			Groups = new Group[ 1 + tables.Count ];

			for (var i = 0; i < tables.Count; i++)
				Groups[i] = new Group {
					Key   = tables[i].Item2,
					Title = tables[i].Item1,
					Elements = users
						.Where(u => tables[i].Item3.Contains(u.Identifier))
						.ToArray()
				};

			// Users that are friends but not in favorites
			Groups[^1] = new Group {
				Key   = "all",
				Title = null,
				Elements = users
					.Where(u => !Groups.Any(g => g != null && g.Elements.Any(e => e.Identifier.Equals(u.Identifier))))
					.ToArray()
			};

			// Remove empty groups 
			Groups = Groups
				.Where(g => g != null && g.Elements.Length > 0)
				.ToArray();

		ended:
			if (updateComponent)
				_component?.UpdateContent();
		}
	}
}