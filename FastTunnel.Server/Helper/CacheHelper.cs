using System.Reactive.Linq;
using Akavache;

namespace FastTunnel.Api.Helper
{
    public static class CacheHelper
    {
        public static IServiceCollection UseAkavache(this IServiceCollection services)
        {
            Registrations.Start("FastTunnel.Server");
            return services;
        }

        public static void StopAkavache()
        {
            BlobCache.Shutdown().WaitAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        /// <summary>
        /// 清空缓存
        /// </summary>
        public static async Task CleanCache()
        {
            await BlobCache.LocalMachine.InvalidateAll();
            await BlobCache.LocalMachine.Vacuum();
        }

        /// <summary>
        /// 清空过期数据
        /// </summary>
        public static async Task CleanInvalidateCache()
        {
            await BlobCache.LocalMachine.Vacuum();
        }

        public static T GetValue<T>(string keyName = "", T defaultVal = default)
        {
            return BlobCache.LocalMachine.GetObject<T>(keyName)
                                                             .Catch(Observable.Return(defaultVal))
                                                             .Wait();
        }

        public static async ValueTask<T> AsyGetValue<T>(string keyName = "", T defaultVal = default)
        {
            return await BlobCache.LocalMachine.GetObject<T>(keyName)
                                                             .Catch(Observable.Return(defaultVal));
        }
        
        public static void SetValue<T>(string keyName = "", T defaultVal = default, DateTimeOffset? absoluteExpiration = null)
        {
            BlobCache.LocalMachine.InsertObject(keyName, defaultVal, absoluteExpiration).Wait();
        }

        public static async ValueTask AsySetValue<T>(string keyName = "", T defaultVal = default, DateTimeOffset? absoluteExpiration = null)
        {
            await BlobCache.LocalMachine.InsertObject(keyName, defaultVal, absoluteExpiration);
        }
    }
}
