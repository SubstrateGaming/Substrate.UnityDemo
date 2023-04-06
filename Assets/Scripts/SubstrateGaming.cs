using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Substrate.NetApi;
using Substrate.NetApi.Model.Extrinsics;
using Substrate.NetApi.Model.Types.Primitive;

using TMPro;
using UnityEngine;

using AjunaExt = Substrate.Ajuna.NET.NetApiExt.Generated;
using AstarExt = Substrate.Astar.NET.NetApiExt.Generated;
using BajunExt = Substrate.Bajun.NET.NetApiExt.Generated;
using KusamaExt = Substrate.Kusama.NET.NetApiExt.Generated;
using PolkadotExt = Substrate.Polkadot.NET.NetApiExt.Generated;
using StatemineExt = Substrate.Statemine.NET.NetApiExt.Generated;
using StatemintExt = Substrate.Statemint.NET.NetApiExt.Generated;

namespace Substrate
{
    public class SubstrateGaming : MonoBehaviour
    {
        public enum SubstrateChains
        {
            Polkadot,
            Kusama,
            Ajuna,
            Astar,
            Bajun,
            Statemine,
            Statemint
        }

        [SerializeField]
        private TMP_Dropdown _dropdown;

        [SerializeField]
        private UnityEngine.UI.Button _connectBtn;

        [SerializeField]
        private UnityEngine.UI.Button _getBlockNumberBtn;

        [SerializeField]
        private TMP_Text _urlLbl;

        [SerializeField]
        private TMP_Text _blockNumber;

        private SubstrateClient _client;

        private bool _running = false;

        private Func<CancellationToken, Task<U32>> SystemStorageNumber { get; set; }

        private Uri GetUri(string url) => new("wss://" + url);

        /// <summary>
        ///
        /// </summary>
        private void Awake()
        {
            _dropdown.AddOptions(Enum.GetNames(typeof(SubstrateChains)).ToList());
        }

        /// <summary>
        ///
        /// </summary>
        private void Start()
        {
            // initialize
            OnValueChanged(_dropdown);

            _getBlockNumberBtn.enabled = false;
            _connectBtn.GetComponentInChildren<TMP_Text>().text = "Connect";
        }

        /// <summary>
        /// Update is called once per frame
        /// </summary>
        private void Update()
        {
            // Method intentionally left empty.
        }

        /// <summary>
        /// Drop down menu initialising a new client specific to each relay- or parachain.
        /// </summary>
        /// <param name="dropdown"></param>
        public async void OnValueChanged(TMP_Dropdown dropdown)
        {
            // disconnect when changing substrate chain
            if (_client != null && _client.IsConnected)
            {
                await _client.CloseAsync();
                LazyUpdate();
            }

            // the system storage calls for most of the substrate based chains are similar, one could use one client to access
            // the similar storage calls, which is good for most of the frame pallets, but it might not work due to different
            // frame versions or different orders in the generation proccess.
            string url = string.Empty;
            switch ((SubstrateChains)dropdown.value)
            {
                case SubstrateChains.Polkadot:
                    {
                        url = "rpc.polkadot.io";
                        _client = new PolkadotExt.SubstrateClientExt(GetUri(url), ChargeTransactionPayment.Default());
                        SystemStorageNumber = ((PolkadotExt.SubstrateClientExt)_client).SystemStorage.Number;
                    }
                    break;

                case SubstrateChains.Kusama:
                    {
                        url = "kusama-rpc.polkadot.io";
                        _client = new KusamaExt.SubstrateClientExt(GetUri(url), ChargeTransactionPayment.Default());
                        SystemStorageNumber = ((KusamaExt.SubstrateClientExt)_client).SystemStorage.Number;
                    }
                    break;

                case SubstrateChains.Ajuna:
                    {
                        url = "rpc-parachain.ajuna.network";
                        _client = new AjunaExt.SubstrateClientExt(GetUri(url), ChargeTransactionPayment.Default());
                        SystemStorageNumber = ((AjunaExt.SubstrateClientExt)_client).SystemStorage.Number;
                    }
                    break;

                case SubstrateChains.Astar:
                    {
                        url = "astar.api.onfinality.io/public-ws";
                        _client = new AstarExt.SubstrateClientExt(GetUri(url), ChargeTransactionPayment.Default());
                        SystemStorageNumber = ((AstarExt.SubstrateClientExt)_client).SystemStorage.Number;
                    }
                    break;

                case SubstrateChains.Bajun:
                    {
                        url = "rpc-parachain.bajun.network";
                        _client = new BajunExt.SubstrateClientExt(GetUri(url), ChargeTransactionPayment.Default());
                        SystemStorageNumber = ((BajunExt.SubstrateClientExt)_client).SystemStorage.Number;
                    }
                    break;

                case SubstrateChains.Statemine:
                    {
                        url = "statemine-rpc.polkadot.io";
                        _client = new StatemineExt.SubstrateClientExt(GetUri(url), ChargeTransactionPayment.Default());
                        SystemStorageNumber = ((StatemineExt.SubstrateClientExt)_client).SystemStorage.Number;
                    }
                    break;

                case SubstrateChains.Statemint:
                    {
                        url = "statemint-rpc.polkadot.io";
                        _client = new StatemintExt.SubstrateClientExt(GetUri(url), ChargeTransactionPayment.Default());
                        SystemStorageNumber = ((StatemintExt.SubstrateClientExt)_client).SystemStorage.Number;
                    }
                    break;

                default:
                    Debug.LogError($"Unhandled enumeration value {dropdown.value}!");
                    break;
            }

            _urlLbl.text = url;
        }

        /// <summary>
        /// Updateing visuals when connection changes
        /// </summary>
        private void LazyUpdate()
        {
            _blockNumber.text = "...";
            _getBlockNumberBtn.enabled = _client.IsConnected;
            _connectBtn.GetComponentInChildren<TMP_Text>().text = _client.IsConnected ? "Disconnect" : "Connect";
            _urlLbl.color = _client.IsConnected ? Color.green : Color.red;
        }

        /// <summary>
        /// Toogeling connection to the substrate chain on and off.
        /// </summary>
        public async void OnToggleConnectAsync()
        {
            if (_running || _client == null)
            {
                return;
            }

            _running = true;

            if (_client.IsConnected)
            {
                await _client.CloseAsync();
            }
            else
            {
                await _client.ConnectAsync(false, true, CancellationToken.None);
            }

            LazyUpdate();

            _running = false;
        }

        /// <summary>
        /// Calling the system pallet storage for the current blocknumber.
        /// </summary>
        public async void OnGetBlockNumberClickAsync()
        {
            if (_running || _client == null || !_client.IsConnected)
            {
                return;
            }

            _running = true;

            if (SystemStorageNumber == null)
            {
                Debug.Log("SystemStorageNumber is null!");
            }

            try
            {
                var blockNumber = await SystemStorageNumber(CancellationToken.None);
                _blockNumber.text = blockNumber != null ? blockNumber.Value.ToString("N0") : "null";
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            _running = false;
        }
    }
}