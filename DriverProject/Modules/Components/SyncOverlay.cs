﻿using UnityEngine.Networking;
using R2API.Networking.Interfaces;
using UnityEngine;
using RoR2;

namespace RobDriver.Modules.Components
{
    internal class SyncOverlay : INetMessage
    {
        private NetworkInstanceId netId;
        private GameObject target;

        public SyncOverlay()
        {
        }

        public SyncOverlay(NetworkInstanceId netId, GameObject target)
        {
            this.netId = netId;
            this.target = target;
        }

        public void Deserialize(NetworkReader reader)
        {
            this.netId = reader.ReadNetworkId();
            this.target = reader.ReadGameObject();
        }

        public void OnReceived()
        {
            var bodyObject = Util.FindNetworkObject(this.netId);
            if (!bodyObject)
            {
                Chat.AddMessage("Fuck");
                return;
            }

            var penis = bodyObject.GetComponent<ModelLocator>();
            if (penis)
            {
                var modelTransform = penis.modelTransform;
                if (modelTransform)
                {
                    var temporaryOverlay = modelTransform.gameObject.AddComponent<TemporaryOverlay>();
                    temporaryOverlay.duration = 4f;
                    temporaryOverlay.destroyComponentOnEnd = true;
                    temporaryOverlay.originalMaterial = Modules.Assets.woundOverlayMat;
                    temporaryOverlay.inspectorCharacterModel = modelTransform.GetComponent<CharacterModel>();
                    temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 4f, 0f);
                    temporaryOverlay.animateShaderAlpha = true;
                }
            }
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(this.netId);
            writer.Write(this.target);
        }
    }
}