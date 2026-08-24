# Shop Service Cache Architecture

## Problem: Stale Cache After Profile Reset

After resetting profile via debug UI:
1. **Purchase limits persist** - "Purchase limit reached" for items never purchased
2. **Gold may accumulate incorrectly** - stale cached state

### Root Cause
`ShopService` cached purchase counts in memory at startup, but didn't clear them on profile reset.

---

## Before: ShopService with Local Cache

```mermaid
flowchart TB
    subgraph Startup
        A[GDScript shop_service.gd] -->|"LoadPurchaseCache()"| B[C# ShopService]
        B -->|"Stores in"| C[("_purchaseCache\n(local Dictionary)")]
    end

    subgraph "Runtime Queries"
        D[GetPurchaseCount] -->|"Reads from"| C
        C -.->|"Cache miss only"| E[ProfileRepository]
        E -->|"Calls"| F[GDScript ProfileRepo]
    end

    subgraph "Profile Reset"
        G[User resets profile] -->|"Clears"| F
        F -->|"Emits"| H((data_changed))
        H -.->|"❌ NOT connected"| B
        C -->|"⚠️ STALE DATA"| I[Purchase limit reached!]
    end

    style C fill:#f66,stroke:#900
    style I fill:#f66,stroke:#900
```

---

## After: Repository-Managed (Stateless Service)

```mermaid
flowchart TB
    subgraph Startup
        A[GDScript shop_service.gd] -->|"No cache loading"| B[C# ShopService]
    end

    subgraph "Runtime Queries"
        D[GetPurchaseCount] -->|"Delegates to"| E[ProfileRepository]
        E -->|"Calls"| F[GDScript ProfileRepo]
        F -->|"Fresh data"| E
    end

    subgraph "Profile Reset"
        G[User resets profile] -->|"Clears"| F
        F -->|"Emits"| H((data_changed))
        H -.->|"Other services react"| I[Quest, encounter, and UI consumers]
    end

    subgraph "Next Query"
        J[GetPurchaseCount] -->|"Reads fresh"| E
        E -->|"✅ Correct: 0"| K[Item purchasable!]
    end

    style E fill:#6f6,stroke:#090
    style K fill:#6f6,stroke:#090
```

---

## Data Flow Comparison

```mermaid
flowchart LR
    subgraph "❌ Before"
        direction TB
        B1[ShopService] -->|"owns"| B2[_purchaseCache]
        B2 -->|"stale on reset"| B3[Bug!]
    end

    subgraph "✅ After"
        direction TB
        A1[ShopService] -->|"queries"| A2[ProfileRepository]
        A2 -->|"delegates"| A3[GDScript ProfileRepo]
        A3 -->|"always fresh"| A4[Correct!]
    end

    style B2 fill:#f66
    style B3 fill:#f66
    style A2 fill:#6f6
    style A4 fill:#6f6
```

---

## Key Principle

| Pattern | Risk | Recommendation |
|---------|------|----------------|
| Service caches profile data | Must remember to clear on reset → bug when forgotten | ❌ Avoid |
| Service queries repository | Reset automatically works | ✅ Preferred |

**Rule**: Services should be **stateless** for profile data. The repository owns all profile state (including caches), so lifecycle is managed automatically.

---

## Files Changed

| File | Change |
|------|--------|
| `scripts/csharp/Meta/Services/Shop/ShopService.cs` | Removed `_purchaseCache`, delegates to `IProfileRepository` |
| `scripts/services/shop_service.gd` | Removed `LoadPurchaseCache()` call |

## Future Pattern

For any new cached state:

```
❌ BAD:  Service caches data in _localCache, forgets to clear on reset
✅ GOOD: Repository owns cache, clears automatically on reset
```
