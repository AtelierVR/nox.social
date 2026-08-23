using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Network;
using Nox.CCK.Utils;
using Nox.Users;
using UnityEngine;
using UnityEngine.UI;

namespace Nox.Social.Clients.Components
{
	public class FriendElementComponent : MonoBehaviour
	{
		public static UniTask<GameObject> ElementPrefab
			=> Client.GetAssetAsync<GameObject>("prefabs/element.prefab");
		public static UniTask<GameObject> ItemPrefab
			=> Client.GetAssetAsync<GameObject>("ui:prefabs/grid_item.prefab");

		public Identifier Identifier = Identifier.Invalid;

		private TextLanguage display;

		public Image bannerImage;
		public GameObject bannerContainer;
		public AspectRatioFitter bannerAspect;

		public Image thumbnailImage;
		public GameObject thumbnailContainer;

		public static async UniTask<FriendElementComponent> Create(Identifier identifier, RectTransform parent, GameObject itemPrefab = null, GameObject elementPrefab = null)
		{
			itemPrefab ??= await ItemPrefab;
			elementPrefab ??= await ElementPrefab;
			var go = await itemPrefab.InstantiateAsync(parent);
			var component = go.AddComponent<FriendElementComponent>();
			component.Identifier = identifier;

			Reference.GetComponent<Button>("button", go)
				.onClick.AddListener(component.OnClicked);

			go = await elementPrefab.InstantiateAsync(Reference.GetComponent<RectTransform>("content", go));
			component.display = Reference.GetComponent<TextLanguage>("display", go);
			component.thumbnailImage = Reference.GetComponent<Image>("thumbnail_image", go);
			component.thumbnailContainer = Reference.GetReference("thumbnail_container", go);
			component.bannerImage = Reference.GetComponent<Image>("banner_image", go);
			component.bannerContainer = Reference.GetReference("banner_container", go);
			component.bannerAspect = Reference.GetComponent<AspectRatioFitter>("banner_ratio", go);

			// Setup NetworkImage callbacks for banner
			var bannerNetworkImage = component.bannerImage.GetOrAddComponent<NetworkImage>();
			bannerNetworkImage.OnSuccess.AddListener(texture => {
				if (texture && texture.height > 0) {
					component.bannerAspect.aspectRatio = (float)texture.width / texture.height;
					component.bannerContainer.SetActive(true);
				}
			});
			bannerNetworkImage.OnError.AddListener(_ => {
				component.bannerContainer.SetActive(false);
			});

			// Setup NetworkImage callbacks for thumbnail
			var thumbnailNetworkImage = component.thumbnailImage.GetOrAddComponent<NetworkImage>();
			thumbnailNetworkImage.OnSuccess.AddListener(_ => {
				component.thumbnailContainer.SetActive(true);
			});
			thumbnailNetworkImage.OnError.AddListener(_ => {
				component.thumbnailContainer.SetActive(false);
			});

			return component;
		}

		private void OnClicked()
		{
			var page   = GetComponentInParent<FriendsComponent>()?.Page;
			var menuId = page?.GetMenu()?.Id ?? 0;
			Client.UiAPI?.SendGoto(menuId, "users", "identifier", Identifier);
		}

		public UniTask UpdateContent(IUser user)
		{
			display.UpdateText("value", new[] { user.Display });
			UpdateBanner(user.Banner);
			UpdateThumbnail(user.Thumbnail);
			return UniTask.CompletedTask;
		}

		private void UpdateThumbnail(string url)
		{
			if (string.IsNullOrEmpty(url))
			{
				thumbnailImage.sprite = null;
				thumbnailContainer.SetActive(false);
				return;
			}

			var networkImage = thumbnailImage.GetOrAddComponent<NetworkImage>();
			networkImage.Url = url;
		}

		private void UpdateBanner(string banner)
		{
			if (string.IsNullOrEmpty(banner))
			{
				bannerImage.sprite = null;
				bannerContainer.SetActive(false);
				return;
			}

			var networkImage = bannerImage.GetOrAddComponent<NetworkImage>();
			networkImage.Url = banner;
		}
	}
}