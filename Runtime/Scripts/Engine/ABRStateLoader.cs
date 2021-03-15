/* ABRStateLoader.cs
 *
 * Copyright (c) 2021 University of Minnesota
 * Authors: Bridger Herman <herma582@umn.edu>
 *
 */

using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using IVLab.Utilities;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace IVLab.ABREngine
{
    public interface IABRStateLoader
    {
        Task<JToken> GetState(string name);

        Task SaveState(string serializedState);
    }

    public class ResourceStateFileLoader : IABRStateLoader
    {
        public ResourceStateFileLoader() { }

        public async Task<JToken> GetState(string fileName)
        {
            TextAsset textAsset = null;
            await UnityThreadScheduler.Instance.RunMainThreadWork(() =>
            {
                string name = Path.GetFileNameWithoutExtension(fileName);
                textAsset = Resources.Load<TextAsset>(name);
            });
            return JObject.Parse(textAsset?.text);
        }

        public async Task SaveState(string serializedState)
        {
            throw new NotImplementedException("States cannot be saved to Resources folder");
        }
    }

    public class HttpStateFileLoader : IABRStateLoader
    {
        public HttpStateFileLoader() { }

        public async Task<JToken> GetState(string url)
        {
            HttpResponseMessage stateResponse = await ABREngine.httpClient.GetAsync(url);
            stateResponse.EnsureSuccessStatusCode();
            string fullStateJson = await stateResponse.Content.ReadAsStringAsync();
            return JObject.Parse(fullStateJson)["state"];
        }

        public async Task SaveState(string serializedState)
        {
            string stateUrl = ABREngine.Instance.Config.Info.serverAddress + ABREngine.Instance.Config.Info.statePathOnServer;
            ByteArrayContent content = new ByteArrayContent(Encoding.UTF8.GetBytes(serializedState));
            HttpResponseMessage stateResponse = await ABREngine.httpClient.PutAsync(stateUrl, content);
            stateResponse.EnsureSuccessStatusCode();
        }
    }
}