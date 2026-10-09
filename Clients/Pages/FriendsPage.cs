using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
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
		private bool _isLoading;
		private bool _loaded;

		public class Group {
			public string Key;
			public string Title;
			public IUser[] Elements;
		}

		public Group[] Groups = Array.Empty<Group>();

		/// <summary>True while the friends list is being fetched.</summary>
		public bool IsLoading
			=> _isLoading;

		/// <summary>True when the last fetch failed.</summary>
		public bool HasError { get; private set; }

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

		// The content is created (and shows a loading placeholder) right before OnOpen,
		// so the fetch is done once in OnDisplay instead of both OnOpen and OnDisplay.
		public void OnOpen(IPage lastPage) { }

		// Only fetch on the first display (or after a failure). Use the refresh button
		// (or OnRefresh) to force an update afterwards, avoiding a full reload on each visit.
		public void OnDisplay(IPage lastPage) {
			if (_loaded || _isLoading)
				return;
			Refresh().Forget();
		}

		public void OnRefresh()
			=> Refresh().Forget();

		public void OnRemove() {
			_component = null;
			_content   = null;
			_loaded    = false;
			_isLoading = false;
		}

		/// <summary>Fetches every page of the friends list (relations resolved to users).</summary>
		private async UniTask<List<IUser>> FetchAllFriends() {
			var users = new List<IUser>();

			var search = await Client.UserAPI.FetchFriends();
			if (search?.Items != null)
				users.AddRange(search.Items.Where(u => u != null));

			while (search != null && search.HasNext()) {
				search = await search.Next();
				if (search == null) {
					Logger.LogWarning("Failed to fetch more friends", tag: nameof(FriendsPage));
					break;
				}
				if (search.Items != null)
					users.AddRange(search.Items.Where(u => u != null));
			}

			return users;
		}

		private async UniTask Refresh() {
			if (_isLoading)
				return;
			_isLoading = true;
			HasError   = false;

			// Show the loading indicator only on the first load to avoid flickering.
			if (!_loaded)
				_component?.SetStatus("friends.loading");

			try {
				// Favorites and friends are independent: fetch them concurrently.
				var favoritesTask = Client.UserAPI.GetFavorites();
				var users         = await FetchAllFriends();
				var table         = await favoritesTask;

				var groups = new List<Group>();

				if (table != null)
					groups.Add(
						new Group {
							Key = table.Key,
							Title = table.Label,
							Elements = users
								.Where(u => table.Values.Contains(u.Identifier))
								.ToArray()
						}
					);

				// Users that are friends but not in favorites
				groups.Add(
					new Group {
						Key = "all",
						Title = null,
						Elements = users
							.Where(u => !groups.Any(g => g.Elements.Any(e => e.Identifier.Equals(u.Identifier))))
							.ToArray()
					}
				);

				// Remove empty groups
				Groups = groups
					.Where(g => g.Elements.Length > 0)
					.ToArray();
				_loaded = true;
			} catch (Exception e) {
				Logger.LogError(e);
				HasError = true;
			} finally {
				_isLoading = false;
			}

			if (_component == null)
				return;

			// When groups are available the component hides the loading indicator itself,
			// right after the group headers are shown (and before the cards are loaded).
			if (HasError)
				_component.SetStatus("friends.error");
			else if (Groups.Length == 0)
				_component.SetStatus("friends.empty");

			await _component.UpdateContent();
		}
	}
}