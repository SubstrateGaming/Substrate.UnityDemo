using Schnorrkel.Keys;
using Substrate.NetApi;
using Substrate.NetApi.Model.Extrinsics;
using Substrate.NetApi.Model.Rpc;
using Substrate.NetApi.Model.Types;
using Substrate.NetApi.Model.Types.Base;
using Substrate.NetApi.Model.Types.Primitive;
using System;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

using AjunaExt = Substrate.Ajuna.NET.NetApiExt.Generated;
using AstarExt = Substrate.Astar.NET.NetApiExt.Generated;
using BajunExt = Substrate.Bajun.NET.NetApiExt.Generated;
using KusamaExt = Substrate.Kusama.NET.NetApiExt.Generated;
using NodeTemplateExt = Substrate.NodeTemplate.NET.NetApiExt.Generated;
using PolkadotExt = Substrate.Polkadot.NET.NetApiExt.Generated;
using StatemineExt = Substrate.Statemine.NET.NetApiExt.Generated;
using StatemintExt = Substrate.Statemint.NET.NetApiExt.Generated;

namespace Substrate
{
    public class SubstrateGaming : MonoBehaviour
    {
        public static MiniSecret MiniSecretAlice => new MiniSecret(Utils.HexToByteArray("0xe5be9a5092b81bca64be81d212e7f2f9eba183bb7a90954f7b76361f6edb5c0a"), ExpandMode.Ed25519);
        public static Account Alice => Account.Build(KeyType.Sr25519, MiniSecretAlice.ExpandToSecret().ToBytes(), MiniSecretAlice.GetPair().Public.Key);

        private ChargeType _chargeTypeDefault;

        public enum SubstrateChains
        {
            Polkadot,
            Kusama,
            Ajuna,
            Astar,
            Bajun,
            Statemine,
            Statemint,
            NodeTemplate
        }

        [SerializeField]
        private TMP_Dropdown _dropdown;

        [SerializeField]
        private UnityEngine.UI.Button _connectBtn;

        [SerializeField]
        private UnityEngine.UI.Button _getBlockNumberBtn;

        [SerializeField]
        private UnityEngine.UI.Button _sendBobBtn;

        [SerializeField]
        private TMP_Text _urlLbl;

        [SerializeField]
        private TMP_Text _blockNumber;

        private SubstrateClient _client;

        private bool _running = false;

        private Func<CancellationToken, Task<U32>> SystemStorageNumber { get; set; }

        private Uri GetUri(string url, bool secure = true) => new($"ws{(secure ? "s" : "")}://" + url);

        private SubstrateChains _currentNetwork;

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
            //_chargeTypeDefault = ChargeTransactionPayment.Default();
            _chargeTypeDefault = ChargeAssetTxPayment.Default();

            // initialize
            OnValueChanged(_dropdown);

            _getBlockNumberBtn.enabled = false;
            _sendBobBtn.enabled = false;
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
            _currentNetwork = (SubstrateChains)dropdown.value;
            switch (_currentNetwork)
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

                case SubstrateChains.NodeTemplate:
                    {
                        url = "127.0.0.1:9944";
                        _client = new NodeTemplateExt.SubstrateClientExt(GetUri(url, false), ChargeTransactionPayment.Default());
                        SystemStorageNumber = ((NodeTemplateExt.SubstrateClientExt)_client).SystemStorage.Number;
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
            _sendBobBtn.enabled = _client.IsConnected && _currentNetwork == SubstrateChains.NodeTemplate;
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

        /// <summary>
        ///
        /// </summary>
        public async void OnSendBobAsync()
        {
            if (_running || _client == null || !_client.IsConnected)
            {
                return;
            }

            _running = true;

            var accountAlice = new Substrate.NodeTemplate.NET.NetApiExt.Generated.Model.sp_core.crypto.AccountId32();
            accountAlice.Create(Utils.GetPublicKeyFrom(Alice.Value));

            var account32 = new Substrate.NodeTemplate.NET.NetApiExt.Generated.Model.sp_core.crypto.AccountId32();
            account32.Create(Utils.GetPublicKeyFrom("5FHneW46xGXgs5mUiveU4sbTyGBzmstUspZC92UhjJM694ty"));

            var multiAddress = new Substrate.NodeTemplate.NET.NetApiExt.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress();
            multiAddress.Create(Substrate.NodeTemplate.NET.NetApiExt.Generated.Model.sp_runtime.multiaddress.MultiAddress.Id, account32);

            var amount = new BaseCom<U128>();
            amount.Create(new BigInteger(42 * Math.Pow(10, 12)));

            var transferKeepAlive = NodeTemplateExt.Storage.BalancesCalls.TransferKeepAlive(multiAddress, amount);

            try
            {
                var subscription = await GenericExtrinsicAsync(_client, Alice, "TransferKeepAlive", transferKeepAlive, CancellationToken.None);

                Debug.Log($"subscription id => {subscription}");
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            _running = false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="extrinsicType"></param>
        /// <param name="extrinsicMethod"></param>
        /// <param name="concurrentTasks"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        internal async Task<string> GenericExtrinsicAsync(SubstrateClient client, Account account, string extrinsicType, Method extrinsicMethod, CancellationToken token)
        {
            string subscription = await client.Author.SubmitAndWatchExtrinsicAsync(ActionExtrinsicUpdate, extrinsicMethod, account, _chargeTypeDefault, 64, token);

            if (subscription == null)
            {
                return null;
            }

            Debug.Log($"Generic extrinsic sent {extrinsicMethod.ModuleName}_{extrinsicMethod.CallName} with {subscription}");

            return subscription;
        }

        public void ActionExtrinsicUpdate(string subscriptionId, ExtrinsicStatus extrinsicUpdate)
        {
            switch (extrinsicUpdate.ExtrinsicState)
            {
                case ExtrinsicState.None:
                    if (extrinsicUpdate.InBlock?.Value.Length > 0)
                    {
                        Debug.Log($"{subscriptionId} => InBlock[{extrinsicUpdate.InBlock.Value}]");
                    }
                    else if (extrinsicUpdate.Finalized?.Value.Length > 0)
                    {
                        Debug.Log($"{subscriptionId} => Finalized[{extrinsicUpdate.Finalized.Value}]");
                    }
                    else
                    {
                        Debug.Log($"{subscriptionId} => {extrinsicUpdate.ExtrinsicState}");
                    }
                    break;

                case ExtrinsicState.Future:
                    Debug.Log($"{subscriptionId} => {extrinsicUpdate.ExtrinsicState}");
                    break;

                case ExtrinsicState.Ready:
                    Debug.Log($"{subscriptionId} => {extrinsicUpdate.ExtrinsicState}");
                    break;

                case ExtrinsicState.Dropped:
                    Debug.Log($"{subscriptionId} => {extrinsicUpdate.ExtrinsicState}");
                    break;

                case ExtrinsicState.Invalid:
                    Debug.Log($"{subscriptionId} => {extrinsicUpdate.ExtrinsicState}");
                    break;
            }
        }
    }
}