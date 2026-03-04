# Infinity SpecOps Attribute Modification System — C# Implementation Specification

## Overview

This document describes the attribute upgrade system for **SpecOps units** in the Infinity tabletop wargame army builder. SpecOps units have a pool of **XP (Experience Points)** that can be spent to incrementally upgrade their combat attributes. Each attribute has a defined number of upgrade tiers, a stat bonus per tier, a hard cap (in some cases), and an XP cost per tier.

---

## Data Model

### SpecOpsUnit

A SpecOps unit tracks:

- **Exp** (`int`): The remaining/available XP budget (modified as upgrades are applied or removed).
- **Attributes**: A dictionary or object containing 7 upgradeable attributes, each identified by an enum/ID.

### Attribute

Each attribute contains:

- **BaseValue** (`int`): The original unmodified stat value for this unit.
- **ModValue** (`int`): The current effective stat value after modifications. Equals `BaseValue` when no upgrades are applied.
- **ModTier** (`int`): The current upgrade tier (0 = no upgrade, higher = more upgraded). Starts at 0.

---

## Attribute Definitions

There are 7 attributes that can be upgraded. Each has specific tier limits, stat bonuses, value caps, and XP costs.

| ID | Name | Max Tier | Tier 1 Bonus | Tier 2 Bonus | Tier 3 Bonus | Hard Cap | Tier 1 XP Cost | Tier 2 XP Cost | Tier 3 XP Cost |
|----|------|----------|-------------|-------------|-------------|----------|----------------|----------------|----------------|
| 1  | CC   | 3        | +2          | +5          | +10         | None     | 2              | 3              | 5              |
| 2  | BS   | 3        | +1          | +2          | +3          | None     | 2              | 3              | 5              |
| 3  | PH   | 2        | +1          | +3          | —           | 14       | 2              | 3              | —              |
| 4  | WIP  | 3        | +1          | +3          | +6          | 15       | 2              | 3              | 5              |
| 5  | ARM  | 2        | +1          | +3          | —           | None     | 5              | 5              | —              |
| 6  | BTS  | 3        | +3          | +6          | +9          | None     | 2              | 3              | 5              |
| 7  | W    | 1        | +1          | —           | —           | 2        | 10             | —              | —              |

### Important Notes

- **"Tier Bonus"** is the **total bonus from base**, not incremental. At Tier 2, the bonus is applied directly from BaseValue (e.g., CC at Tier 2 = BaseValue + 5, NOT BaseValue + 2 + 5).
- **"Hard Cap"** means ModValue cannot exceed this number regardless of BaseValue + Bonus. Apply as: `ModValue = Min(BaseValue + Bonus, HardCap)`.
- **"XP Cost"** is the cost to go **from the previous tier to this tier**. These are incremental costs, not cumulative. Going from Tier 0 → Tier 1 costs Tier 1's XP. Going from Tier 1 → Tier 2 costs Tier 2's XP.

---

## Behavior: ModifyAttribute(attributeId, attribute, isIncrement)

### Parameters

- `attributeId` (int/enum, 1–7): Which attribute to modify.
- `attribute`: The attribute object being modified (has BaseValue, ModValue, ModTier).
- `isIncrement` (bool): `true` to upgrade, `false` to downgrade.

### Algorithm

```
1. If ModTier is 0 and isIncrement is false → do nothing (can't go below 0).
2. If ModTier is at MaxTier and isIncrement is true → do nothing (already maxed).
3. If the attribute has a hard cap and ModValue is already at the cap and isIncrement is true → do nothing.

4. Determine the XP delta:
   - If isIncrement is true:
       newTier = ModTier + 1
       xpCost = +CostForTier[newTier]  (positive = deducted from budget)
   - If isIncrement is false:
       xpCost = -CostForTier[ModTier]  (negative = refunded to budget)
       newTier = ModTier - 1

5. Update ModTier to newTier.

6. Calculate ModValue:
   - If newTier == 0: ModValue = BaseValue
   - Else: ModValue = Min(BaseValue + BonusForTier[newTier], HardCap or int.MaxValue)

7. Update the XP pool:
   - Exp += xpCost  (adding because xpCost is negative when spending; 
                      OR subtract if you define cost as positive)
```

**Note on XP sign convention:** In the original code, `r` is computed as a positive value when upgrading and added to `this.addSO.exp`. This means `exp` tracks the **total XP spent** (going up). If your implementation tracks **remaining XP**, reverse the sign: subtract on upgrade, add on downgrade.

---

## Per-Attribute Tier Data (for lookup table implementation)

The cleanest C# implementation would use a lookup table rather than a switch statement. Here is the data:

### CC (ID = 1)
```
Tier 0: Bonus =  0, Cumulative XP =  0
Tier 1: Bonus = +2, Tier Cost = 2 XP
Tier 2: Bonus = +5, Tier Cost = 3 XP
Tier 3: Bonus = +10, Tier Cost = 5 XP
No hard cap.
```

### BS (ID = 2)
```
Tier 0: Bonus =  0, Cumulative XP =  0
Tier 1: Bonus = +1, Tier Cost = 2 XP
Tier 2: Bonus = +2, Tier Cost = 3 XP
Tier 3: Bonus = +3, Tier Cost = 5 XP
No hard cap.
```

### PH (ID = 3)
```
Tier 0: Bonus =  0, Cumulative XP =  0
Tier 1: Bonus = +1, Tier Cost = 2 XP
Tier 2: Bonus = +3, Tier Cost = 3 XP
Hard cap: 14
```

### WIP (ID = 4)
```
Tier 0: Bonus =  0, Cumulative XP =  0
Tier 1: Bonus = +1, Tier Cost = 2 XP
Tier 2: Bonus = +3, Tier Cost = 3 XP
Tier 3: Bonus = +6, Tier Cost = 5 XP
Hard cap: 15
```

### ARM (ID = 5)
```
Tier 0: Bonus =  0, Cumulative XP =  0
Tier 1: Bonus = +1, Tier Cost = 5 XP
Tier 2: Bonus = +3, Tier Cost = 5 XP
No hard cap.
```

### BTS (ID = 6)
```
Tier 0: Bonus =  0, Cumulative XP =  0
Tier 1: Bonus = +3, Tier Cost = 2 XP
Tier 2: Bonus = +6, Tier Cost = 3 XP
Tier 3: Bonus = +9, Tier Cost = 5 XP
No hard cap.
```

### W (Wounds, ID = 7)
```
Tier 0: Bonus =  0, Cumulative XP =  0
Tier 1: Bonus = +1, Tier Cost = 10 XP
Hard cap: 2
```

---

## Suggested C# Structure

```
enum AttributeType { CC = 1, BS = 2, PH = 3, WIP = 4, ARM = 5, BTS = 6, W = 7 }

class AttributeTierInfo {
    int Bonus;       // Total bonus from base at this tier
    int XpCost;      // XP cost to go FROM the previous tier TO this tier
}

class AttributeDefinition {
    AttributeType Type;
    int MaxTier;
    int? HardCap;                         // null if no cap
    List<AttributeTierInfo> Tiers;         // Index 0 = tier 0 (no bonus, no cost), etc.
}

class UnitAttribute {
    int BaseValue;
    int ModValue;
    int ModTier;
}

class SpecOpsUnit {
    int Exp;
    Dictionary<AttributeType, UnitAttribute> Attributes;
    Dictionary<AttributeType, AttributeDefinition> Definitions;  // static/shared

    void ModifyAttribute(AttributeType type, bool isIncrement);
}
```

---

## Edge Cases and Validation

1. **Never allow ModTier below 0.** If `ModTier == 0` and `isIncrement == false`, return immediately.
2. **Never allow ModTier above MaxTier.** If `ModTier == MaxTier` and `isIncrement == true`, return immediately.
3. **Hard cap check on increment:** For PH, WIP, and W, also block the increment if `ModValue` is already at the hard cap (the original code checks this independently of the tier check).
4. **XP validation (optional):** The original code does NOT check if the unit has enough XP before applying an upgrade. You may want to add this: `if (isIncrement && Exp < tierCost) return;`
5. **Initialization:** When a unit is first loaded, `ModTier` should be 0 and `ModValue` should equal `BaseValue` for all attributes.
