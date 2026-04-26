using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
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
			UpdateBanner(user.Banner).Forget();
			UpdateThumbnail(user.Thumbnail).Forget();
			return UniTask.CompletedTask;
		}

		private CancellationTokenSource _thumbnailTokenSource;
		private CancellationTokenSource _bannerTokenSource;

		private async UniTask UpdateThumbnail(string url)
		{
			if (_thumbnailTokenSource != null)
			{
				_thumbnailTokenSource?.Cancel();
				_thumbnailTokenSource?.Dispose();
			}

			_thumbnailTokenSource = new CancellationTokenSource();
			
			if (!string.IsNullOrEmpty(url))
			{
				if(thumbnailImage.sprite == null)
					thumbnailContainer.SetActive(false);

				var texture = await Client.NetworkAPI
					.FetchTexture(url)
					.AttachExternalCancellation(_thumbnailTokenSource.Token);
				if (texture)
				{
					thumbnailImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
					thumbnailContainer.SetActive(true);
				}
				else
				{ 
					thumbnailImage.sprite = null;
					thumbnailContainer.SetActive(false);
				}
			}
			else
			{
				thumbnailImage.sprite = null;
				thumbnailContainer.SetActive(false);
			}

			_thumbnailTokenSource = null;
		}

		private async UniTask UpdateBanner(string banner)
		{
			if (_bannerTokenSource != null)
			{
				_bannerTokenSource.Cancel();
				_bannerTokenSource.Dispose();
			}

			_bannerTokenSource = new CancellationTokenSource();
			if (!string.IsNullOrEmpty(banner))
			{
				if (bannerImage.sprite == null)
					bannerContainer.SetActive(false);
					
				var texture = await Client.NetworkAPI
					.FetchTexture(banner)
					.AttachExternalCancellation(_bannerTokenSource.Token);
				if (texture && texture.height > 0)
				{
					bannerImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
					bannerAspect.aspectRatio = (float)texture.width / texture.height;
					bannerContainer.SetActive(true);
				}
				else
				{
					bannerImage.sprite = null;
					bannerContainer.SetActive(false);
				}
			}
			else
			{
				bannerImage.sprite = null;
				bannerContainer.SetActive(false);
			}

			_bannerTokenSource = null;
		}
	}
}