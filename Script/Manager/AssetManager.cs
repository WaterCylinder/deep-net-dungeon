using Godot;
using Godot.Collections;

namespace DeepNetDungeon.Manager
{
    /// <summary>
    /// 资源管理器 - 处理所有资源加载和卸载
    /// </summary>
    public partial class AssetManager : SingletonManager<AssetManager>
    {
        private Dictionary<string, Resource> _loadedResources = new();
        private Dictionary<string, Texture2D> _cachedTextures = new();
        private Dictionary<string, AudioStream> _cachedAudio = new();

        /// <summary>
        /// 异步加载资源
        /// </summary>
        public async System.Threading.Tasks.Task<T> LoadAsync<T>(string path) where T : Resource
        {
            // 留空：异步加载资源
            return null;
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        public T Load<T>(string path) where T : Resource
        {
            // 留空：同步加载资源
            return null;
        }

        /// <summary>
        /// 加载纹理
        /// </summary>
        public Texture2D LoadTexture(string path)
        {
            // 留空：加载纹理
            return null;
        }

        /// <summary>
        /// 加载音频
        /// </summary>
        public AudioStream LoadAudio(string path)
        {
            // 留空：加载音频
            return null;
        }

        /// <summary>
        /// 卸载资源
        /// </summary>
        public void Unload(string path)
        {
            // 留空：卸载资源
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public override void Cleanup()
        {
            _loadedResources.Clear();
            _cachedTextures.Clear();
            _cachedAudio.Clear();
            base.Cleanup();
        }
    }
}
