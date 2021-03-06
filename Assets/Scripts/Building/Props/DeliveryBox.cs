using System.Collections;
using System.Linq;
using Mirror;
using Sim.Entities;
using Sim.Enums;
using Sim.Interactables;
using Sim.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace Sim.Building {
    public class DeliveryBox : Props {
        [Header("Settings")]
        [SerializeField]
        private Transform clapTransform;

        [SerializeField]
        private Quaternion openedClapRotation;

        [SerializeField]
        private GameObject package;

        [SerializeField]
        private AudioClip alertSound;

        [Header("Debug")]
        private Delivery[] deliveries;

        private AudioSource _audioSource;

        [SyncVar(hook = nameof(RefreshDeliveriesQuantity))]
        [SerializeField]
        private uint deliveryCount;

        public delegate void UnPackageEvent(Delivery delivery);

        public static event UnPackageEvent UnPackage;

        public override void OnStartClient() {
            base.OnStartClient();
            
            this._audioSource = GetComponent<AudioSource>();
            PropsContentUI.OnSelect += OnSelectDelivery;
        }

        public override void OnStopClient() {
            base.OnStopClient();
            
            PropsContentUI.OnSelect -= OnSelectDelivery;
        }

        public override void OnStartServer() {
            base.OnStartServer();
            this.CheckDeliveries();
        }

        public void CheckDeliveries() {
            StartCoroutine(this.RetrieveDeliveries());
        }

        [Server]
        public IEnumerator RetrieveDeliveries() {
            UnityWebRequest request = ApiManager.Instance.RetrieveDeliveriesRequest(GetComponentInParent<ApartmentController>().HomeData.Tenant);
            
            yield return request.SendWebRequest();

            if (request.responseCode == 200) {
                DeliveryResponse deliveryResponse = JsonUtility.FromJson<DeliveryResponse>(request.downloadHandler.text);
                this.deliveries = deliveryResponse.Deliveries.ToArray();
                this.deliveryCount = (uint)this.deliveries.Length;
                Debug.Log($"Delivery box has {this.deliveryCount} deliveries");
            } else {
                Debug.Log("No deliveries found");
                this.Deliveries = new Delivery[0];
                this.deliveryCount = 0;
            }
        }

        public void RefreshDeliveriesQuantity(uint oldValue, uint newValue) {
            this.deliveryCount = newValue;
            this.UpdateGraphics();
        }

        protected override void Execute(Action action) {
            if (action.Type.Equals(ActionTypeEnum.OPEN) && GetComponentInParent<ApartmentController>().IsTenant(PlayerController.Local.CharacterData)) {
                CmdLook();
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdLook(NetworkConnectionToClient sender = null) {
            Debug.Log($"Server: netId {sender.identity.netId} wants to look into delivery box");
            TargetOpenDeliveryBox(sender, this.Deliveries);
        }

        [Server]
        public void RefreshPlayerUI(NetworkConnectionToClient sender) {
            this.TargetOpenDeliveryBox(sender, this.deliveries);
        }

        [TargetRpc]
        public void TargetOpenDeliveryBox(NetworkConnection target, Delivery[] data) {
            this.Deliveries = data;
            PlayerController.Local.Interact(this);
            DefaultViewUI.Instance.ShowPropsContentUI(deliveries.Select(x => x.DisplayName()).ToArray());
        }

        public override void StopInteraction() {
            DefaultViewUI.Instance.HidePropsContentUI();
        }

        private void OnSelectDelivery(int idx) {
            if (!this.ApartmentController.IsTenant(PlayerController.Local.CharacterData)) {
                return;
            }
            
            if (this.deliveries == null || this.deliveries.Length == 0) {
                PlayerController.Local.Idle();
            } else {
                UnPackage?.Invoke(this.deliveries[idx]);
            }
        }

        public Delivery[] Deliveries {
            get => deliveries;
            set => deliveries = value;
        }

        private void UpdateGraphics() {
            if (this.deliveryCount > 0) {
                this.clapTransform.localRotation = this.openedClapRotation;
                this.package.SetActive(true);
            } else {
                this.clapTransform.localRotation = Quaternion.Euler(0, 0, 0);
                this.package.SetActive(false);
            }
        }
    }
}