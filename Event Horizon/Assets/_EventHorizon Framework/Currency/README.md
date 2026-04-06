# EventHorizon.Currency

```text
   .   *      .      /\       .
      .    *        /==\   .
   .       .       |::::|     *
      ORBITAL OBSERVATION DECK
```

| Document Field | Value |
|---|---|
| Layer | Runtime Feature Module |
| Assembly | `EventHorizon.Currency` |
| Primary Entry Point | `CurrencyModuleSO` |
| Depends On | `EventHorizon.Core` |
| Technical Role | currency definitions, wallet storage, write-path validation, transaction signaling |

`EventHorizon.Currency` is a small, asset-driven economy layer. It is designed around a strict rule:

- the wallet stores balances
- the module performs all writes
- other systems observe the results through events

This keeps the economy predictable and stops unrelated scripts from mutating balances directly.

## Assembly dependency

- `EventHorizon.Currency` depends on `EventHorizon.Core`

## Module contents

### Definitions

- `CurrencyDefinitionSO`
  - Unique currency identity.
  - Stores `CurrencyID`, display name, icon, and initial balance.

### Runtime state

- `CurrencyWalletSO`
  - The source of truth for balances at runtime.
  - Initializes balances from all registered definitions in `OnEnable()`.
  - Implements `ISaveable`, so the save module can persist it.

### Service layer

- `CurrencyModuleSO`
  - The write gateway for the economy.
  - Exposes:
    - `GetBalance`
    - `HasSufficient`
    - `AddCurrency`
    - `SpendCurrency`
  - Raises transaction or insufficient-funds events after operations.

### Events

- `CurrencyEventChannelSO`
  - `OnCurrencyChanged`
  - `OnInsufficientFunds`
- `CurrencyTransactionEvent`
  - payload containing currency, old balance, new balance, and delta
- `CurrencyTransactionEventSO`
  - typed event asset for transaction payloads
- `ICurrencyProvider`
  - read/write contract exposed by the module

## How the module works

At startup:

1. `CurrencyWalletSO.OnEnable()` clears runtime balances.
2. Every registered currency definition seeds its balance into the wallet.
3. `CurrencyModuleSO` becomes the public API for balance changes.

When currency is added:

1. the module validates currency, amount, and wallet
2. it reads the old balance
3. it writes the new balance into the wallet
4. it raises `OnCurrencyChanged`

When currency is spent:

1. the module validates input
2. it checks affordability through `HasSufficient`
3. if the player cannot afford it:
   - `OnInsufficientFunds` is raised
   - the balance stays unchanged
4. if the player can afford it:
   - the new balance is written
   - a transaction event is raised with a negative delta

## Setup tutorial

### 1. Create currency definitions

For each currency:

1. Right-click in the Project window.
2. Choose `Create > EventHorizon > Currency > Currency Definition`.
3. Fill in:
   - `Currency ID`
   - `Display Name`
   - `Icon`
   - `Initial Balance`

Use stable IDs. The wallet save format stores balances by `CurrencyID`, not by asset GUID.

### 2. Create the wallet

1. Right-click.
2. Choose `Create > EventHorizon > Currency > Currency Wallet`.
3. Add all `CurrencyDefinitionSO` assets to `Registered Currencies`.

The wallet is the actual runtime storage and the saveable object for persistence.

### 3. Create the currency event channel

1. Create a typed transaction event asset.
2. Create a parameterless insufficient-funds event asset.
3. Create `CurrencyEventChannelSO`.
4. Assign both event references.

### 4. Create the module

1. Right-click.
2. Choose `Create > EventHorizon > Currency > Currency Module`.
3. Assign:
   - `Wallet`
   - `Event Channel`
4. Add the module asset to your `ModuleRegistry`.

### 5. Optional: connect save support

If you also use `EventHorizon.Save`:

1. create a `SaveModuleSO`
2. add the `CurrencyWalletSO` to the save module's `Registered Saveables`

Because the wallet implements `ISaveable`, it will automatically be captured and restored.

## Typical usage

### Read balances

```csharp
int coins = currencyModule.GetBalance(coinsDefinition);
bool canAffordSkin = currencyModule.HasSufficient(gemsDefinition, 50);
```

### Add currency

```csharp
currencyModule.AddCurrency(coinsDefinition, 100);
```

### Spend currency

```csharp
bool success = currencyModule.SpendCurrency(gemsDefinition, 25);
```

Never write through the wallet directly from game code. The wallet is storage, not the public domain API.

## UI integration pattern

The currency module itself does not push values into UI. A common pattern is:

1. listen to `OnCurrencyChanged`
2. map the transaction to one or more `IntVariableSO` assets
3. let `UIViewBase` bind to those variables

That gives you:

- no direct UI dependency in the economy module
- reactive UI updates
- easy mocking and editor testing

Example bridge:

```csharp
private void OnCurrencyChanged(CurrencyTransactionEvent evt)
{
    if (evt.Currency == _coinsDefinition)
    {
        _coinsVariable.SetValue(evt.NewBalance);
    }
}
```

## Save behavior

`CurrencyWalletSO` captures data as a JSON payload of currency ID / balance pairs. On restore, it reapplies the balances into the runtime dictionary.

Important implications:

- changing `CurrencyID` after shipping can orphan older saves
- removing a currency definition from the wallet means it will no longer be seeded on enable
- newly added currencies will use their definition's initial balance until a save restore overwrites them

## Recommended conventions

- Use singular wallet assets per game economy unless you have a real need for separation.
- Keep `CurrencyID` short, stable, and machine-friendly.
- Use event listeners or variable bridges for UI instead of direct polling in many places.
- Keep purchasable systems dependent on `SpendCurrency` return values, not optimistic assumptions.

## Common pitfalls

- Changing `CurrencyID` after save data already exists.
- Bypassing the module and writing directly to the wallet.
- Forgetting to add a currency definition to the wallet.
- Using negative values with `AddCurrency` or `SpendCurrency`; the module expects positive amounts.
- Expecting UI to update automatically without an event listener or variable bridge.

<table width="100%">
  <tr>
    <td bgcolor="#57A8A8" width="25%"></td>
    <td bgcolor="#30B06E" width="25%"></td>
    <td bgcolor="#D1AD29" width="25%"></td>
    <td bgcolor="#C7262E" width="25%"></td>
  </tr>
</table>

```text
          _|_
---@----(_)--@---
          | |
          ./ \.

    .-----.   .-----.   .-----.
    | Fe  |   | Li  |   | Na  |
    '-----'   '-----'   '-----'
     BLOOD      METH     TEARS
```
