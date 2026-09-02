using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;

namespace EpicsAIO.Utilities;

[Injectable(InjectionType.Singleton)]
public class BaseGameItemEdits(
    ISptLogger<BaseGameItemEdits> logger,
    TemplateTable templateTable,
    SlotHelper slotHelper) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        EditFilters();
        return Task.CompletedTask;
    }

    private void EditFilters()
    {
        Dictionary<MongoId, TemplateItem> items = templateTable.Items;
        foreach (var (val3, val4) in items)
        {
            switch (val3.ToString())
            {
			case "5447a9cd4bdc2dbd208b4567":
			{
				ModifySlotFilters(val4, 2, 0, new List<MongoId> { new MongoId("5bb20d53d4351e4502010a69") });
				break;
			}
			case "5bb2475ed4351e00853264e3":
			{
				ModifySlotFilters(val4, 2, 0, new List<MongoId> { new MongoId("5c0e2f26d174af02a9625114"), new MongoId("55d355e64bdc2d962f8b4569"), new MongoId("5d4405aaa4b9361e6a4e6bd3"), new MongoId("5c07a8770db8340023300450"), new MongoId("59bfe68886f7746004266202"), new MongoId("63f5ed14534b2c3d5479a677"), new MongoId("c6aa3fe86a9fc7ea6c220c2f"), new MongoId("cc85761fc4963442077f9460") });
				ModifySlotFilters(val4, 3, 0, new List<MongoId> { new MongoId("5a33ca0fc4a282000d72292f"), new MongoId("5c0faeddd174af02a962601f"), new MongoId("5649be884bdc2d79388b4577"), new MongoId("5d120a10d7ad1a4e1026ba85"), new MongoId("602e3f1254072b51b239f713"), new MongoId("5c793fb92e221644f31bfb64"), new MongoId("5c793fc42e221600114ca25d"), new MongoId("638de3603a1a4031d8260b8c") });
				break;
			}
			case "65266fd43341ed9aa903dd56":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5e208b9842457a4a7a33d074"), new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "6513eff1e06849f06c0957d4":
			{
				ModifySlotFilters(val4, 1, 0, new List<MongoId> { new MongoId("5e208b9842457a4a7a33d074"), new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5c7e5f112e221600106f4ede":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("40c62378fa93a829532ecc5e"), new MongoId("6d9f22a75064ebb92b3ece1c") });
				break;
			}
			case "5d25a6538abbc306c62e630d":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("6529243824cbe3c74a05e5c1"), new MongoId("6529302b8c26af6326029fb7") }, isCartridge: true);
				break;
			}
			case "5d25a4a98abbc30b917421a4":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("6529243824cbe3c74a05e5c1"), new MongoId("6529302b8c26af6326029fb7") }, isCartridge: true);
				break;
			}
			case "5d25a7b88abbc3054f3e60bc":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("6529243824cbe3c74a05e5c1"), new MongoId("6529302b8c26af6326029fb7") }, isCartridge: true);
				break;
			}
			case "5ce69cbad7f00c00b61c5098":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("6529243824cbe3c74a05e5c1"), new MongoId("6529302b8c26af6326029fb7") }, isCartridge: true);
				break;
			}
			case "5d25a6a48abbc306c62e6310":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("6529243824cbe3c74a05e5c1"), new MongoId("6529302b8c26af6326029fb7") }, isCartridge: true);
				break;
			}
			case "5d25af8f8abbc3055079fec5":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("6529243824cbe3c74a05e5c1"), new MongoId("6529302b8c26af6326029fb7") }, isCartridge: true);
				break;
			}
			case "5cf12a15d7f00c05464b293f":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("6529243824cbe3c74a05e5c1"), new MongoId("6529302b8c26af6326029fb7") }, isCartridge: true);
				break;
			}
			case "5bfeaa0f0db834001b734927":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("6529243824cbe3c74a05e5c1"), new MongoId("6529302b8c26af6326029fb7") }, isCartridge: true);
				break;
			}
			case "5bfea7ad0db834001c38f1ee":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("6529243824cbe3c74a05e5c1"), new MongoId("6529302b8c26af6326029fb7") }, isCartridge: true);
				break;
			}
			case "59984ab886f7743e98271174":
			{
				ModifySlotFilters(val4, 1, 0, new List<MongoId> { new MongoId("6761779c48fa5c377e06fc3f"), new MongoId("9d387502b50d1f4b0fb8b0ce") });
				break;
			}
			case "59f9cabd86f7743a10721f46":
			{
				ModifySlotFilters(val4, 1, 0, new List<MongoId> { new MongoId("6761779c48fa5c377e06fc3f"), new MongoId("9d387502b50d1f4b0fb8b0ce") });
				break;
			}
			case "5ab8e9fcd8ce870019439434":
			{
				ModifySlotFilters(val4, 6, 0, new List<MongoId> { new MongoId("6761779c48fa5c377e06fc3f"), new MongoId("9d387502b50d1f4b0fb8b0ce") });
				ModifySlotFilters(val4, 2, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "57dc2fa62459775949412633":
			{
				ModifySlotFilters(val4, 1, 0, new List<MongoId> { new MongoId("6761779c48fa5c377e06fc3f"), new MongoId("9d387502b50d1f4b0fb8b0ce") });
				ModifySlotFilters(val4, 4, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "583990e32459771419544dd2":
			{
				ModifySlotFilters(val4, 1, 0, new List<MongoId> { new MongoId("6761779c48fa5c377e06fc3f"), new MongoId("9d387502b50d1f4b0fb8b0ce") });
				ModifySlotFilters(val4, 4, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5839a40f24597726f856b511":
			{
				ModifySlotFilters(val4, 1, 0, new List<MongoId> { new MongoId("6761779c48fa5c377e06fc3f"), new MongoId("9d387502b50d1f4b0fb8b0ce") });
				ModifySlotFilters(val4, 4, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5bf3e0490db83400196199af":
			{
				ModifySlotFilters(val4, 6, 0, new List<MongoId> { new MongoId("6761779c48fa5c377e06fc3f"), new MongoId("9d387502b50d1f4b0fb8b0ce") });
				ModifySlotFilters(val4, 2, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "618b9682a3884f56c957ca78":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("81ee14e532991b2b9993ae0e"), new MongoId("ac738256c846acc25b183a80"), new MongoId("26f02172906cbaa1ae78e57d"), new MongoId("87690b1fadc788a959d42e46") });
				break;
			}
			case "618ba92152ecee1505530bd3":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("81ee14e532991b2b9993ae0e"), new MongoId("ac738256c846acc25b183a80"), new MongoId("26f02172906cbaa1ae78e57d"), new MongoId("87690b1fadc788a959d42e46") });
				break;
			}
			case "5580223e4bdc2d1c128b457f":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("64748d02d1c009260702b526") });
				break;
			}
			case "55d3632e4bdc2d972f8b4569":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("45b07217916365a3171c079e") });
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "55d35ee94bdc2d61338b4568":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5d440b9fa4b93601354d480c":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068"), new MongoId("ac14dc7aa887301d799e3b2b") });
				break;
			}
			case "5d440b93a4b9364276578d4b":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068"), new MongoId("ac14dc7aa887301d799e3b2b") });
				break;
			}
			case "5c0e2f94d174af029f650d56":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068"), new MongoId("ac14dc7aa887301d799e3b2b") });
				break;
			}
			case "63d3ce0446bd475bcb50f55f":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "63d3d44a2a49307baf09386d":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068"), new MongoId("ac14dc7aa887301d799e3b2b") });
				break;
			}
			case "5cf67cadd7f00c065a5abab7":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5f2aa43ba9b91d26f20ae6d2":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "628b9c37a733087d0d7fe84b":
			{
				ModifySlotFilters(val4, 1, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "628b5638ad252a16da6dd245":
			{
				ModifySlotFilters(val4, 1, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5bf3e03b0db834001d2c4a9c":
			{
				ModifySlotFilters(val4, 2, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5ac4cd105acfc40016339859":
			{
				ModifySlotFilters(val4, 2, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5644bd2b4bdc2d3b4c8b4572":
			{
				ModifySlotFilters(val4, 2, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5ac66cb05acfc40198510a10":
			{
				ModifySlotFilters(val4, 2, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5ac66d015acfc400180ae6e4":
			{
				ModifySlotFilters(val4, 2, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5ac66d2e5acfc43b321d4b53":
			{
				ModifySlotFilters(val4, 2, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5ac66d725acfc43b321d4b60":
			{
				ModifySlotFilters(val4, 2, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5ac66d9b5acfc4001633997a":
			{
				ModifySlotFilters(val4, 2, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "62e7c7f3c34ea971710c32fc":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "630e39c3bd357927e4007c15":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "6333f05d1bc0e6217a0e9d34":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5c48a2852e221602b21d5923":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5dcbe9431e1f4616d354987e":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "622b379bf9cfc87d675d2de5":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "622b3858034a3e17ad0b81f5":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "622b38c56762c718e457e246":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "61702be9faa1272e431522c3":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5bb20d92d4351e00853263eb":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5bb20d9cd4351e00334c9d8a":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5bb20da5d4351e0035629dbf":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5bb20dadd4351e00367faeff":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5c6d85e02e22165df16b81f4":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5fbbfabed5cb881a7363194e":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5fbbfacda56d053a3543f799":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "60658776f2cb2e02a42ace2b":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "6065878ac9cf8012264142fd":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "628a60ae6b1d481ff772e9c8":
			{
				ModifySlotFilters(val4, 1, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5b099a765acfc47a8607efe3":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5b7be1125acfc4001876c0e5":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5b7be1265acfc400161d0798":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "618168b350224f204c1da4d8":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "6183b0711cb55961fa0fdcad":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "6183b084a112697a4b3a6e6c":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "6183fc15d3a39d50044c13e9":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "6183fd911cb55961fa0fdce9":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "6183fd9e8004cc50514c358f":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5bfebc320db8340019668d79":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5d2703038abbc3105103d94c":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5cf79389d7f00c10941a0c4d":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5cf67a1bd7f00c06585fb6f3":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5cf79599d7f00c10875d9212":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5ab3afb2d8ce87001660304d":
			{
				ModifySlotFilters(val4, 1, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5a34f7f1c4a2826c6e06d75d":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5a34fae7c4a2826c6e06d760":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5df917564a9f347bc92edca3":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5dfa397fb11454561e39246c":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "660126f7c752a02bbe05e688":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "66012788c752a02bbe05e68e":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "6601279cc752a02bbe05e692":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "66225d88a1c7e3b81600c76f":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5beec1bd0db834001e6006f3":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "5beec2820db834001b095426":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068") });
				break;
			}
			case "652910565ae2ae97b80fdf35":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5cc125555c98bf150a4fd068"), new MongoId("612e0d3767085e45ef14057f"), new MongoId("5b7d693d5acfc43bca706a3d"), new MongoId("5a34fd2bc4a282329a73b4c5"), new MongoId("6065c6e7132d4d12c81fd8e1"), new MongoId("5d1f819086f7744b355c219b"), new MongoId("5dcbe965e4ed22586443a79d"), new MongoId("5d026791d7ad1a04a067ea63"), new MongoId("5dfa3cd1b33c0951220c079b"), new MongoId("6130c43c67085e45ef1405a1"), new MongoId("5cdd7685d7f00c000f260ed2"), new MongoId("5c878e9d2e2216000f201903"), new MongoId("5d02677ad7ad1a04a15c0f95"), new MongoId("5bbdb8bdd4351e4502011460"), new MongoId("5cdd7693d7f00c0010373aa5"), new MongoId("607ffb988900dc2d9a55b6e4"), new MongoId("615d8eb350224f204c1da1cf"), new MongoId("612e0e3c290d254f5e6b291d"), new MongoId("5d443f8fa4b93678dd4a01aa"), new MongoId("5c7954d52e221600106f4cc7"), new MongoId("5fbc22ccf24b94483f726483"), new MongoId("59bffc1f86f77435b128b872"), new MongoId("5cf78496d7f00c065703d6ca"), new MongoId("5d270ca28abbc31ee25ee821"), new MongoId("5d270b3c8abbc3105335cfb8"), new MongoId("5fbe7618d6fa9c00c571bb6c"), new MongoId("628a66b41d5e41750e314f34"), new MongoId("618178aa1cb55961fa0fdc80"), new MongoId("6642f63667f5cb56a00662eb") });
				break;
			}
			case "57ac965c24597706be5f975c":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("eeb62fba336b644c26813276") });
				break;
			}
			case "57aca93d2459771f2c7e26db":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("eeb62fba336b644c26813276") });
				break;
			}
			case "5c0e2f26d174af02a9625114":
			{
				ReplaceSlotFilters(val4, 3, 0, new HashSet<MongoId>
				{
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("61817865d3a39d50044c13a4")
				});
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5ae30bad5acfc400185c2dc4") });
				break;
			}
			case "55d355e64bdc2d962f8b4569":
			{
				ReplaceSlotFilters(val4, 3, 0, new HashSet<MongoId>
				{
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("61817865d3a39d50044c13a4")
				});
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5ae30bad5acfc400185c2dc4") });
				break;
			}
			case "5d4405aaa4b9361e6a4e6bd3":
			{
				ReplaceSlotFilters(val4, 3, 0, new HashSet<MongoId>
				{
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("61817865d3a39d50044c13a4")
				});
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5ae30bad5acfc400185c2dc4") });
				break;
			}
			case "5c07a8770db8340023300450":
			{
				ReplaceSlotFilters(val4, 3, 0, new HashSet<MongoId>
				{
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("61817865d3a39d50044c13a4")
				});
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5ae30bad5acfc400185c2dc4") });
				break;
			}
			case "59bfe68886f7746004266202":
			{
				ReplaceSlotFilters(val4, 3, 0, new HashSet<MongoId>
				{
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("61817865d3a39d50044c13a4")
				});
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5ae30bad5acfc400185c2dc4") });
				break;
			}
			case "63f5ed14534b2c3d5479a677":
			{
				ReplaceSlotFilters(val4, 3, 0, new HashSet<MongoId>
				{
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("61817865d3a39d50044c13a4")
				});
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5ae30bad5acfc400185c2dc4") });
				break;
			}
			case "6529119424cbe3c74a05e5bb":
			{
				ReplaceSlotFilters(val4, 3, 0, new HashSet<MongoId>
				{
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("61817865d3a39d50044c13a4")
				});
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5ae30bad5acfc400185c2dc4") });
				break;
			}
			case "5bb20d53d4351e4502010a69":
			{
				ReplaceSlotFilters(val4, 3, 0, new HashSet<MongoId>
				{
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("61817865d3a39d50044c13a4")
				});
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5ae30bad5acfc400185c2dc4") });
				break;
			}
			case "5c488a752e221602b412af63":
			{
				ReplaceSlotFilters(val4, 5, 0, new HashSet<MongoId>
				{
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("61817865d3a39d50044c13a4")
				});
				ModifySlotFilters(val4, 4, 0, new List<MongoId> { new MongoId("5ae30bad5acfc400185c2dc4") });
				break;
			}
			case "5dcbd56fdbd3d91b3e5468d5":
			{
				ReplaceSlotFilters(val4, 5, 0, new HashSet<MongoId>
				{
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("61817865d3a39d50044c13a4")
				});
				ModifySlotFilters(val4, 4, 0, new List<MongoId> { new MongoId("5ae30bad5acfc400185c2dc4") });
				break;
			}
			case "5df8e4080b92095fd441e594":
			{
				ReplaceSlotFilters(val4, 3, 0, new HashSet<MongoId>
				{
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("61817865d3a39d50044c13a4")
				});
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5ae30bad5acfc400185c2dc4") });
				break;
			}
			case "5fc278107283c4046c581489":
			{
				ReplaceSlotFilters(val4, 3, 0, new HashSet<MongoId>
				{
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("61817865d3a39d50044c13a4")
				});
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5ae30bad5acfc400185c2dc4") });
				break;
			}
			case "602e63fb6335467b0c5ac94d":
			{
				ReplaceSlotFilters(val4, 3, 0, new HashSet<MongoId>
				{
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("61817865d3a39d50044c13a4")
				});
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5ae30bad5acfc400185c2dc4") });
				break;
			}
			case "606587a88900dc2d9a55b659":
			{
				ReplaceSlotFilters(val4, 3, 0, new HashSet<MongoId>
				{
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("61817865d3a39d50044c13a4")
				});
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5ae30bad5acfc400185c2dc4") });
				break;
			}
			case "6165adcdd3a39d50044c120f":
			{
				ReplaceSlotFilters(val4, 2, 0, new HashSet<MongoId>
				{
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("61817865d3a39d50044c13a4")
				});
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5ae30bad5acfc400185c2dc4") });
				break;
			}
			case "6165aeedfaa1272e431521e3":
			{
				ReplaceSlotFilters(val4, 2, 0, new HashSet<MongoId>
				{
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("61817865d3a39d50044c13a4")
				});
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5ae30bad5acfc400185c2dc4") });
				break;
			}
			case "61713a8fd92c473c770214a4":
			{
				ReplaceSlotFilters(val4, 3, 0, new HashSet<MongoId>
				{
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("61817865d3a39d50044c13a4")
				});
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5ae30bad5acfc400185c2dc4") });
				break;
			}
			case "618405198004cc50514c3594":
			{
				ReplaceSlotFilters(val4, 2, 0, new HashSet<MongoId>
				{
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("61817865d3a39d50044c13a4")
				});
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5ae30bad5acfc400185c2dc4") });
				break;
			}
			case "618426d96c780c1e710c9b9f":
			{
				ReplaceSlotFilters(val4, 2, 0, new HashSet<MongoId>
				{
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("61817865d3a39d50044c13a4")
				});
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5ae30bad5acfc400185c2dc4") });
				break;
			}
			case "62811fbf09427b40ab14e767":
			{
				ReplaceSlotFilters(val4, 2, 0, new HashSet<MongoId>
				{
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("61817865d3a39d50044c13a4")
				});
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("5ae30bad5acfc400185c2dc4") });
				break;
			}
			case "5ae30bad5acfc400185c2dc4":
			{
				ModifySlotFilters(val4, 0, 0, new List<MongoId> { new MongoId("66713838ca123f9df8e7584e"), new MongoId("23878a19adddf4afbbcc0537") });
				val4.Properties.Prefab.Path = "assets/content/items/mods/sights_rear/sight_rear_ar15_colt_carry_hande_std.bundle";
				val4.Parent = new MongoId("55818ad54bdc2ddc698b4569");
				val4.Properties.ConflictingItems = new HashSet<MongoId>
				{
					new MongoId("61817865d3a39d50044c13a4"),
					new MongoId("5bb20e49d4351e3bac1212de"),
					new MongoId("5ba26b17d4351e00367f9bdd"),
					new MongoId("5dfa3d7ac41b2312ea33362a"),
					new MongoId("5c1780312e221602b66cc189"),
					new MongoId("5fb6564947ce63734e3fa1da"),
					new MongoId("5bc09a18d4351e003562b68e"),
					new MongoId("5c18b9192e2216398b5a8104"),
					new MongoId("5fc0fa957283c4046c58147e"),
					new MongoId("5894a81786f77427140b8347"),
					new MongoId("55d5f46a4bdc2d1b198b4567"),
					new MongoId("e8d92f66b5cf4553048c5f5b")
				};
				break;
			}
			case "55d5f46a4bdc2d1b198b4567":
				val4.Parent = new MongoId("55818ad54bdc2ddc698b4569");
				break;
			case "5bfd4c980db834001b73449d":
				val4.Parent = new MongoId("55818ad54bdc2ddc698b4569");
				break;
			case "5ae099925acfc4001a5fc7b3":
				val4.Parent = new MongoId("55818ad54bdc2ddc698b4569");
				break;
			case "59d650cf86f7741b846413a4":
				val4.Parent = new MongoId("55818ad54bdc2ddc698b4569");
				break;
			case "5a0eb980fcdbcb001a3b00a6":
				val4.Parent = new MongoId("55818ad54bdc2ddc698b4569");
				break;
			case "628a7b23b0f75035732dd565":
				val4.Parent = new MongoId("55818ad54bdc2ddc698b4569");
				break;
			case "5ac733a45acfc400192630e2":
				val4.Parent = new MongoId("55818ad54bdc2ddc698b4569");
				break;
			case "5649b0544bdc2d1b2b8b458a":
				val4.Parent = new MongoId("55818ad54bdc2ddc698b4569");
				break;
			case "5ac72e475acfc400180ae6fe":
				val4.Parent = new MongoId("55818ad54bdc2ddc698b4569");
				break;
			case "649ec2cec93611967b03495e":
				val4.Parent = new MongoId("55818ad54bdc2ddc698b4569");
				break;
			case "5bf3f59f0db834001a6fa060":
				val4.Parent = new MongoId("55818ad54bdc2ddc698b4569");
				break;
			case "651450ce0e00edc794068371":
				val4.Properties.BFirerate = 750.0;
				break;
			case "615d8d878004cc50514c3233":
				val4.Properties.ConflictingItems = new HashSet<MongoId>();
				break;
			case "55f84c3c4bdc2d5f408b4576":
				slotHelper.EnsureSlot(val4, "mod_tactical_003", "55d30c4c4bdc2db4468b457e", false, false, 0);
				slotHelper.EnsureSlot(val4, "mod_tactical_004", "55d30c4c4bdc2db4468b457e", false, false, 0);
				slotHelper.AddIdsToNamedSlot(val4, "mod_tactical_003", new string[2] { "6a17976b6252dc8bcb000001", "68b26fcb9db8d58487000001" });
				slotHelper.AddIdsToNamedSlot(val4, "mod_tactical_004", new string[2] { "6a17976b6252dc8bcb000001", "68b26fcb9db8d58487000001" });
				break;
			case "588b56d02459771481110ae2":
				slotHelper.EnsureSlot(val4, "mod_tactical_003", "55d30c4c4bdc2db4468b457e", false, false, 0);
				slotHelper.EnsureSlot(val4, "mod_tactical_004", "55d30c4c4bdc2db4468b457e", false, false, 0);
				slotHelper.AddIdsToNamedSlot(val4, "mod_tactical_003", new string[2] { "6a17976b6252dc8bcb000001", "68b26fcb9db8d58487000001" });
				slotHelper.AddIdsToNamedSlot(val4, "mod_tactical_004", new string[2] { "6a17976b6252dc8bcb000001", "68b26fcb9db8d58487000001" });
				break;
			case "5c9a25172e2216000f20314e":
				slotHelper.EnsureSlot(val4, "mod_tactical_003", "55d30c4c4bdc2db4468b457e", false, false, 0);
				slotHelper.EnsureSlot(val4, "mod_tactical_004", "55d30c4c4bdc2db4468b457e", false, false, 0);
				slotHelper.AddIdsToNamedSlot(val4, "mod_tactical_003", new string[2] { "6a17976b6252dc8bcb000001", "68b26fcb9db8d58487000001" });
				slotHelper.AddIdsToNamedSlot(val4, "mod_tactical_004", new string[2] { "6a17976b6252dc8bcb000001", "68b26fcb9db8d58487000001" });
				break;
			case "5c9a26332e2216001219ea70":
				val4.Properties.ConflictingItems = new HashSet<MongoId>();
				slotHelper.EnsureSlot(val4, "mod_tactical_003", "55d30c4c4bdc2db4468b457e", false, false, 0);
				slotHelper.EnsureSlot(val4, "mod_tactical_004", "55d30c4c4bdc2db4468b457e", false, false, 0);
				slotHelper.AddIdsToNamedSlot(val4, "mod_tactical_003", new string[2] { "6a17976b6252dc8bcb000001", "68b26fcb9db8d58487000001" });
				slotHelper.AddIdsToNamedSlot(val4, "mod_tactical_004", new string[2] { "6a17976b6252dc8bcb000001", "68b26fcb9db8d58487000001" });
				ReplaceSlotFilters(val4, 0, 0, new HashSet<MongoId>
				{
					new MongoId("6a1889fce506c69d80000001"),
					new MongoId("6a17d1bd98c6bf6da000000c")
				});
				break;
			case "55d459824bdc2d892f8b4573":
			{
				ModifySlotFilters(val4, 1, 0, new List<MongoId> { new MongoId("68b26fcb9db8d58487000001"), new MongoId("6a17976b6252dc8bcb000001") });
				ModifySlotFilters(val4, 3, 0, new List<MongoId> { new MongoId("68b26fcb9db8d58487000001"), new MongoId("6a17976b6252dc8bcb000001") });
				break;
			}
			}
		}
	}

	private void ReplaceSlotFilters(TemplateItem item, int slotIndex, int filterIndex, HashSet<MongoId> ids)
	{
		Slot slotAtIndex = GetSlotAtIndex(item, slotIndex);
		SlotFilter slotFilterAtIndex = GetSlotFilterAtIndex(slotAtIndex, filterIndex);
		slotFilterAtIndex.Filter = ids;
	}

	private void ModifySlotFilters(TemplateItem item, int slotIndex, int filterIndex, List<MongoId> ids, bool isCartridge = false)
	{
		Slot slotAtIndex = GetSlotAtIndex(item, slotIndex, isCartridge);
		SlotFilter slotFilterAtIndex = GetSlotFilterAtIndex(slotAtIndex, filterIndex);
		slotFilterAtIndex.Filter.UnionWith(ids);
	}

	private Slot GetSlotAtIndex(TemplateItem item, int index, bool isCartridge = false)
	{
		object obj;
		if (!isCartridge)
		{
			TemplateItemProperties properties = item.Properties;
			obj = ((properties == null) ? null : properties.Slots?.ToArray());
		}
		else
		{
			TemplateItemProperties properties2 = item.Properties;
			obj = ((properties2 == null) ? null : properties2.Cartridges?.ToArray());
		}
		Slot[] array = (Slot[])obj;
		if (index >= 0 && index < array?.Length)
		{
			return array[index];
		}
		throw new IndexOutOfRangeException("Index on item slot property `" + item.Name + "` is out of range");
	}

	private SlotFilter GetSlotFilterAtIndex(Slot slot, int index)
	{
		SlotProperties properties = slot.Properties;
		SlotFilter[] array = ((properties == null) ? null : properties.Filters?.ToArray()) ?? Array.Empty<SlotFilter>();
		if (index >= 0 && index < array.Length)
		{
			return array[index];
		}
		throw new IndexOutOfRangeException("Index on slot property `" + slot.Name + "` is out of range");
	}
}
