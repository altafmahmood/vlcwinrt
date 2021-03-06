﻿/**********************************************************************
 * VLC for WinRT
 **********************************************************************
 * Copyright © 2013-2014 VideoLAN and Authors
 *
 * Licensed under GPLv2+ and MPLv2
 * Refer to COPYING file of the official project for license
 **********************************************************************/

using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VLC_WINRT.Common;
using VLC_WINRT.Model;
using VLC_WINRT.ViewModels;
using Windows.Storage;
using Windows.Storage.Streams;
using VLC_WINRT.ViewModels.MainPage;

namespace VLC_WINRT.Utility.Helpers.MusicLibrary
{
    public static class ArtistInformationsHelper
    {

        private static async Task<bool> DownloadArtistPictureFromDeezer(MusicLibraryViewModel.ArtistItem artist)
        {
            var deezerClient = new DeezerClient();
            var deezerArtist = await deezerClient.GetArtistInfo(artist.Name);
            if (deezerArtist == null) return false;
            if (deezerArtist.Images == null) return false;
            try
            {
                var clientPic = new HttpClient();
                HttpResponseMessage responsePic = await clientPic.GetAsync(deezerArtist.Images.LastOrDefault().Url);
                string uri = responsePic.RequestMessage.RequestUri.AbsoluteUri;
                // A cheap hack to avoid using Deezers default image for bands.
                if (uri.Equals("http://cdn-images.deezer.com/images/artist//400x400-000000-80-0-0.jpg"))
                {
                    return false;
                }
                byte[] img = await responsePic.Content.ReadAsByteArrayAsync();
                InMemoryRandomAccessStream streamWeb = new InMemoryRandomAccessStream();
                DataWriter writer = new DataWriter(streamWeb.GetOutputStreamAt(0));
                writer.WriteBytes(img);
                await writer.StoreAsync();
                StorageFolder artistPic = await ApplicationData.Current.LocalFolder.CreateFolderAsync("artistPic",
                    CreationCollisionOption.OpenIfExists);
                string fileName = artist.Name + "_" + "dPi";
                var file = await artistPic.CreateFileAsync(fileName + ".jpg", CreationCollisionOption.OpenIfExists);
                var raStream = await file.OpenAsync(FileAccessMode.ReadWrite);

                using (var thumbnailStream = streamWeb.GetInputStreamAt(0))
                {
                    using (var stream = raStream.GetOutputStreamAt(0))
                    {
                        await RandomAccessStream.CopyAsync(thumbnailStream, stream);
                    }
                }
                StorageFolder appDataFolder = ApplicationData.Current.LocalFolder;
                string supposedPictureUriLocal = appDataFolder.Path + "\\artistPic\\" + artist.Name + "_" + "dPi" + ".jpg";
                await DispatchHelper.InvokeAsync(() => artist.Picture = supposedPictureUriLocal);
                return true;
            }
            catch (Exception)
            {
                Debug.WriteLine("Error getting or saving art from deezer.");
                return false;
            }
        }

        private static async Task<bool> DownloadArtistPictureFromLastFm(MusicLibraryViewModel.ArtistItem artist)
        {
            var lastFmClient = new LastFmClient();
            var lastFmArtist = await lastFmClient.GetArtistInfo(artist.Name);
            if (lastFmArtist == null) return false;
            try
            {
                var clientPic = new HttpClient();
                var imageElement = lastFmArtist.Images.LastOrDefault(node => !string.IsNullOrEmpty(node.Url));
                if (imageElement == null) return false;
                HttpResponseMessage responsePic = await clientPic.GetAsync(imageElement.Url);
                byte[] img = await responsePic.Content.ReadAsByteArrayAsync();
                InMemoryRandomAccessStream streamWeb = new InMemoryRandomAccessStream();

                DataWriter writer = new DataWriter(streamWeb.GetOutputStreamAt(0));
                writer.WriteBytes(img);

                await writer.StoreAsync();

                StorageFolder artistPic = await ApplicationData.Current.LocalFolder.CreateFolderAsync("artistPic",
                    CreationCollisionOption.OpenIfExists);
                string fileName = artist.Name + "_" + "dPi";

                var file = await artistPic.CreateFileAsync(fileName + ".jpg", CreationCollisionOption.OpenIfExists);
                var raStream = await file.OpenAsync(FileAccessMode.ReadWrite);

                using (var thumbnailStream = streamWeb.GetInputStreamAt(0))
                {
                    using (var stream = raStream.GetOutputStreamAt(0))
                    {
                        await RandomAccessStream.CopyAsync(thumbnailStream, stream);
                    }
                }
                StorageFolder appDataFolder = ApplicationData.Current.LocalFolder;
                string supposedPictureUriLocal = appDataFolder.Path + "\\artistPic\\" + artist.Name + "_" + "dPi" + ".jpg";
                DispatchHelper.InvokeAsync(() => artist.Picture = supposedPictureUriLocal);
                return true;
            }
            catch (Exception)
            {
                Debug.WriteLine("Error getting or saving art from LastFm.");
                return false;
            }
        }

        public static async Task GetArtistPicture(MusicLibraryViewModel.ArtistItem artist)
        {
            StorageFolder appDataFolder = ApplicationData.Current.LocalFolder;
            string supposedPictureUriLocal = appDataFolder.Path + "\\artistPic\\" + artist.Name + "_" + "dPi" + ".jpg";
            if (NativeOperationsHelper.FileExist(supposedPictureUriLocal))
            {
                DispatchHelper.InvokeAsync(() =>
                {
                    artist.Picture = supposedPictureUriLocal;
                });
            }
            else
            {
                var gotArt = await DownloadArtistPictureFromDeezer(artist);
                if (!gotArt)
                {
                    await DownloadArtistPictureFromLastFm(artist);
                }
            }
        }

        public static async Task GetArtistTopAlbums(MusicLibraryViewModel.ArtistItem artist)
        {
            try
            {
                Debug.WriteLine("Getting TopAlbums from LastFM API");
                var lastFmClient = new LastFmClient();
                var albums = await lastFmClient.GetArtistTopAlbums(artist.Name);
                Debug.WriteLine("Receive TopAlbums from LastFM API");
                if (albums != null)
                {
                    artist.OnlinePopularAlbumItems = albums;
                    artist.IsOnlinePopularAlbumItemsLoaded = true;
                }
            }
            catch
            {
                Debug.WriteLine("Error getting top albums from artist.");
            }
        }

        public static async Task GetArtistSimilarsArtist(MusicLibraryViewModel.ArtistItem artist)
        {
            try
            {
                var lastFmClient = new LastFmClient();
                var similarArtists = await lastFmClient.GetSimilarArtists(artist.Name);
                if (similarArtists != null)
                {
                    artist.OnlineRelatedArtists = similarArtists;
                    artist.IsOnlineRelatedArtistsLoaded = true;
                }
            }
            catch
            {
                Debug.WriteLine("Error getting similar artists from this artist.");
            }
        }

        public static async Task GetArtistBiography(MusicLibraryViewModel.ArtistItem artist)
        {
            string biography = string.Empty;
            try
            {
                var lastFmClient = new LastFmClient();
                var artistInformation = await lastFmClient.GetArtistInfo(artist.Name);
                biography = artistInformation.Biography;
            }
            catch
            {
                Debug.WriteLine("Failed to get artist biography from LastFM. Returning nothing.");
            }
            artist.Biography = System.Net.WebUtility.HtmlDecode(biography);
        }
    }
}
