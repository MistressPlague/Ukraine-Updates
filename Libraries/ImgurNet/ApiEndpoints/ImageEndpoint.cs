﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ImgurNet.Authentication;
using ImgurNet.Exceptions;
using ImgurNet.Models;
using ImgurNet.Web;

namespace ImgurNet.ApiEndpoints
{
	public class ImageEndpoint : BaseEndpoint
	{
		#region EndPoints

		internal const string UploadImageUrl =		"image";
		internal const string ImageUrl =			"image/{0}";
		internal const string FavouriteImageUrl =	"image/{0}/favorite";

		#endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="imgur"></param>
		public ImageEndpoint(Imgur imgur)
		{
			ImgurClient = imgur;
		}

		/// <summary>
		/// Get information about an image.
		/// </summary>
		/// <param name="imageId">The Id of the image you want details of.</param>
		/// <returns>The image data.</returns>
		public async Task<ImgurResponse<Image>> GetImageDetailsAsync(string imageId)
		{
			if (ImgurClient.Authentication == null)
				throw new InvalidAuthenticationException("Authentication can not be null. Set it in the main Imgur class.");

			return await Request.SubmitImgurRequestAsync<Image>(Request.HttpMethod.Get, String.Format(ImageUrl, imageId), ImgurClient.Authentication);
		}

		/// <summary>
		/// Deletes an image from imgur. You get the deletion hash from the initial image response when you upload an image, or 
		/// from <see cref="GetImageDetailsAsync"/> if you are signed in and own that image;
		/// </summary>
		/// <param name="imageDeletionHash">The image deletion hash</param>
		public async Task<ImgurResponse<Boolean>> DeleteImageAsync(string imageDeletionHash)
		{
			if (ImgurClient.Authentication == null)
				throw new InvalidAuthenticationException("Authentication can not be null. Set it in the main Imgur class.");

			return await Request.SubmitImgurRequestAsync<Boolean>(Request.HttpMethod.Delete, String.Format(ImageUrl, imageDeletionHash), ImgurClient.Authentication);
		}

		/// <summary>
		/// Updates an image that was previously uploaded. ImageId can be the Image Id, if you're signed in as the uploader, or the DeleteHash if you are not.
		/// </summary>
		/// <param name="imageId">The ImageId (or deletion hash) of the image to be edited.</param>
		/// <param name="title">The string you want to set as the title of image.</param>
		/// <param name="description">The string you want to set as the description of image.</param>
		/// <returns>A boolean indicating if the transaction was successful.</returns>
		public async Task<ImgurResponse<Boolean>> UpdateImageDetailsAsync(string imageId, string title = null, string description = null)
		{
			if (ImgurClient.Authentication == null)
				throw new InvalidAuthenticationException("Authentication can not be null. Set it in the main Imgur class.");

			var keyPairs = new List<KeyValuePair<string, string>>();
			if (title != null) keyPairs.Add(new KeyValuePair<string, string>("title", title));
			if (description != null) keyPairs.Add(new KeyValuePair<string, string>("description", description));
			var multi = new FormUrlEncodedContent(keyPairs.ToArray());

			return
				await
					Request.SubmitImgurRequestAsync<Boolean>(Request.HttpMethod.Post, String.Format(ImageUrl, imageId),
						ImgurClient.Authentication, content: multi);
		}

		/// <summary>
		/// Adds/Removes an image from the authenticated user's favourites. Must be authenticated using <see cref="OAuth2Authentication"/> to call this Endpoint.
		/// </summary>
		/// <param name="imageId">The ImageId of the image you want to favourite.</param>
		/// <returns>An bool declaring if the item is now favourited.</returns>
		public async Task<ImgurResponse<Boolean>> FavouriteImageAsync(string imageId)
		{
			if (ImgurClient.Authentication == null)
				throw new InvalidAuthenticationException("Authentication can not be null. Set it in the main Imgur class.");

			if (!(ImgurClient.Authentication is OAuth2Authentication))
				throw new InvalidAuthenticationException("You need to use OAuth2Authentication to call this Endpoint.");

			var response =
				await
					Request.SubmitImgurRequestAsync<String>(Request.HttpMethod.Post, String.Format(FavouriteImageUrl, imageId),
						ImgurClient.Authentication);

			return new ImgurResponse<Boolean>
			{
				Data = (response.Data.ToLowerInvariant() == "favorited"),
				Status = response.Status,
				Success = response.Success
			};
		}

		#region Upload Base64 Image

		/// <summary>
		/// 
		/// </summary>
		/// <param name="base64ImageData"></param>
		/// <param name="albumId"></param>
		/// <param name="name"></param>
		/// <param name="title"></param>
		/// <param name="description"></param>
		/// <returns></returns>
		public async Task<ImgurResponse<Image>> UploadImageFromBase64Async(string base64ImageData,
			string albumId = null, string name = null, string title = null, string description = null)
		{
			return await UploadImageFromBinaryAsync(Convert.FromBase64String(base64ImageData), albumId, name, title, description);
		}

		#endregion

		#region Upload Image From Url

		/// <summary>
		/// 
		/// </summary>
		/// <param name="url"></param>
		/// <param name="albumId"></param>
		/// <param name="name"></param>
		/// <param name="title"></param>
		/// <param name="description"></param>
		/// <returns></returns>
		public async Task<ImgurResponse<Image>> UploadImageFromUrlAsync(string url,
			string albumId = null, string name = null, string title = null, string description = null)
		{
			if (ImgurClient.Authentication == null)
				throw new InvalidAuthenticationException("Authentication can not be null. Set it in the main Imgur class.");

			var keyPairs = new List<KeyValuePair<string, string>>
			{
				new("image", url),
				new("type", "url")
			};
			if (albumId != null) keyPairs.Add(new KeyValuePair<string, string>(albumId, albumId));
			if (name != null) keyPairs.Add(new KeyValuePair<string, string>("name", name));
			if (title != null) keyPairs.Add(new KeyValuePair<string, string>("title", title));
			if (description != null) keyPairs.Add(new KeyValuePair<string, string>("description", description));
			var multi = new FormUrlEncodedContent(keyPairs.ToArray());

			return await Request.SubmitImgurRequestAsync<Image>(Request.HttpMethod.Post, UploadImageUrl, ImgurClient.Authentication, content: multi);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="albumId"></param>
		/// <param name="name"></param>
		/// <param name="title"></param>
		/// <param name="description"></param>
		/// <returns></returns>
		public async Task<ImgurResponse<Image>> UploadImageFromUrlAsync(Uri uri,
			string albumId = null, string name = null, string title = null, string description = null)
		{
			return await UploadImageFromUrlAsync(uri.ToString(), albumId, name, title, description);
		}

		#endregion

		#region Upload Image From Binary

		/// <summary>
		/// 
		/// </summary>
		/// <param name="imageBinary"></param>
		/// <param name="albumId"></param>
		/// <param name="name"></param>
		/// <param name="title"></param>
		/// <param name="description"></param>
		/// <returns></returns>
		public async Task<ImgurResponse<Image>> UploadImageFromBinaryAsync(byte[] imageBinary,
			string albumId = null, string name = null, string title = null, string description = null)
		{
			if (ImgurClient.Authentication == null)
				throw new InvalidAuthenticationException("Authentication can not be null. Set it in the main Imgur class.");

			var keyPairs = new Dictionary<string, string>();
			if (albumId != null) keyPairs.Add(albumId, albumId);
			if (name != null) keyPairs.Add("name", name);
			if (title != null) keyPairs.Add("title", title);
			if (description != null) keyPairs.Add("description", description);
			var stream = new StreamContent(new MemoryStream(imageBinary));

			return await Request.SubmitImgurRequestAsync<Image>(Request.HttpMethod.Post, UploadImageUrl, ImgurClient.Authentication, keyPairs, stream);
		}

		#endregion
	}
}
