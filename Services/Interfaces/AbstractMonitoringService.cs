﻿using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YWB.AntidetectAccountParser.Helpers;
using YWB.AntidetectAccountParser.Model;

namespace YWB.AntidetectAccountParser.Services.Interfaces
{
    public abstract class AbstractMonitoringService
    {
        protected string _token;
        protected string _apiUrl;

        protected abstract Task SetTokenAndApiUrlAsync();
        protected abstract void AddAuthorization(RestRequest r);
        protected abstract Task<List<AccountsGroup>> GetExistingGroupsAsync();
        protected abstract Task<AccountsGroup> AddNewGroupAsync();
        protected abstract Task<List<Proxy>> GetExistingProxiesAsync();
        protected abstract Task<string> AddProxyAsync(Proxy p);
        protected abstract Task<bool> AddAccountAsync(FacebookAccount acc, AccountsGroup g, string proxyId);
        public async Task AddAccountsAsync(List<FacebookAccount> accounts)
        {
            var groups = await GetExistingGroupsAsync();
            Console.WriteLine("Do you want to add your accounts to group/tag?");
            var group = await SelectHelper.SelectWithCreateAsync(groups, g => g.Name, AddNewGroupAsync, true);
            Console.WriteLine("Getting existing proxies...");
            var existingProxies = (await GetExistingProxiesAsync()).ToDictionary(p => p, p => p.Id);
            foreach (var acc in accounts)
            {
                string proxyId;
                if (existingProxies.ContainsKey(acc.Proxy))
                {
                    proxyId = existingProxies[acc.Proxy];
                    Console.WriteLine($"Found existing proxy for {acc.Proxy}!");
                }
                else
                {
                    Console.WriteLine($"Adding proxy {acc.Proxy}...");
                    proxyId = await AddProxyAsync(acc.Proxy);
                    existingProxies.Add(acc.Proxy, proxyId);
                    Console.WriteLine($"Proxy {acc.Proxy} added!");
                }
                Console.WriteLine($"Adding account {acc.Name}...");
                var success = await AddAccountAsync(acc, group, proxyId);
                if (success)
                    Console.WriteLine($"Account {acc.Name} added!");
            }
        }

        protected async Task<T> ExecuteRequestAsync<T>(RestRequest r)
        {
            if (string.IsNullOrEmpty(_token) || string.IsNullOrEmpty(_apiUrl))
                await SetTokenAndApiUrlAsync();
            var rc = new RestClient(_apiUrl);
            AddAuthorization(r);
            var resp = await rc.ExecuteAsync(r, new CancellationToken());
            T res = default(T);
            try
            {
                res = JsonConvert.DeserializeObject<T>(resp.Content);
            }
            catch (Exception)
            {
                Console.WriteLine($"Error deserializing {resp.Content} to {typeof(T)}");
                throw;
            }
            return res;
        }

    }
}
