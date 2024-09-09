using RoR2.ContentManagement;
using System.Linq;

namespace RobDriver.Modules
{
    internal class ContentPacks : IContentPackProvider
    {
        internal ContentPack contentPack = new();
        public string identifier => DriverPlugin.MODUID;

        public void Initialize() => ContentManager.collectContentPackProviders += ContentManager_collectContentPackProviders;

        private void ContentManager_collectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider) => addContentPackProvider(this);

        public System.Collections.IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            this.contentPack.identifier = this.identifier;
            contentPack.bodyPrefabs.Add([.. Prefabs.bodyPrefabs]);
            contentPack.buffDefs.Add([.. Buffs.buffDefs]);
            contentPack.effectDefs.Add([.. Assets.effectDefs]);
            contentPack.entityStateTypes.Add([.. States.entityStates]);
            contentPack.masterPrefabs.Add([.. Prefabs.masterPrefabs]);
            contentPack.networkSoundEventDefs.Add([.. Assets.networkSoundEventDefs]);
            contentPack.projectilePrefabs.Add([.. Prefabs.projectilePrefabs]);
            contentPack.unlockableDefs.Add([.. Assets.unlockableDefs]);
            contentPack.skillDefs.Add([.. Skills.skillDefs]);
            contentPack.skillFamilies.Add([.. Skills.skillFamilies]);
            contentPack.survivorDefs.Add([.. Prefabs.survivorDefinitions]);

            args.ReportProgress(1f);
            yield break;
        }

        public System.Collections.IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(this.contentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public System.Collections.IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }
    }
}