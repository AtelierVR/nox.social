using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.Network;
using Nox.Social.Clients.Pages;
using Nox.UI;
using Nox.Users;
using Nox.Tables;
using UnityEngine;

namespace Nox.Social.Clients {
	public class Client : IClientModInitializer {
		static internal Client Instance { get; private set; }
		static internal IClientModCoreAPI API { get; private set; }

		static internal IUiAPI UiAPI
			=> API?.ModAPI
				?.GetMod("ui")
				?.GetInstance<IUiAPI>();

		static internal IUserAPI UserAPI
			=> API?.ModAPI
				?.GetMod("users")
				?.GetInstance<IUserAPI>();

		static internal INetworkAPI NetworkAPI
			=> API?.ModAPI
				?.GetMod("network")
				?.GetInstance<INetworkAPI>();

		static internal ITableAPI TableAPI
			=> API?.ModAPI
				?.GetMod("tables")
				?.GetInstance<ITableAPI>();

		public static T GetAsset<T>(string path) where T : UnityEngine.Object
			=> API?.AssetAPI?.GetAsset<T>(path);

		public static UniTask<T> GetAssetAsync<T>(string path) where T : UnityEngine.Object
			=> API?.AssetAPI?.GetAssetAsync<T>(path) ?? UniTask.FromResult<T>(null);

		private EventSubscription[] _events = Array.Empty<EventSubscription>();

		public void OnInitializeClient(IClientModCoreAPI api) {
			Instance = this;
			API      = api;
			_events = new[] {
				API.EventAPI.Subscribe("menu_goto", OnGoto)
			};
		}

		public void OnDisposeClient() {
			foreach (var e in _events)
				API?.EventAPI?.Unsubscribe(e);
			_events  = Array.Empty<EventSubscription>();
			API      = null;
			Instance = null;
		}

		private static void OnGoto(EventData context) {
			if (!context.TryGet(0, out int menuId))
				return;
			if (!context.TryGet(1, out string pageKey))
				return;

			var menu = UiAPI?.Get<IMenu>(menuId);
			if (menu == null)
				return;

			IPage page = null;
			if (pageKey == FriendsPage.GetStaticKey())
				page = FriendsPage.Create(menu, context.Data[2..]);

			if (page != null)
				API.EventAPI.Emit("menu_display", menuId, page);
		}
	}
}