using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Utils;

namespace WeaponCustomizer.Server;

[Injectable(TypePriority = OnLoadOrder.Routers + 1)]
public class WeaponCustomizerRouter(JsonUtil jsonUtil, WeaponCustomizer weaponCustomizer)
    : StaticRouter(
        jsonUtil,
        [
            new RouteAction<SaveRequestData>(
                "/weaponcustomizer/save",
                async (url, info, sessionId, output, cancellationToken) => await weaponCustomizer.SaveCustomizations(info, cancellationToken)
            ),
            new RouteAction(
                "/weaponcustomizer/load",
                async (url, info, sessionId, output, cancellationToken) => new ValueTask<string>(jsonUtil.Serialize(weaponCustomizer.Database))
            )
        ]
    )
{ }
