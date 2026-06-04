# FindBestCraftableItem

- 类名：`FindBestCraftableItem`
- 节点类型：动作节点
- 分类：SurvivalEngine/Player
- 基类：`ActionTask`
- 源文件：`Assets/SurvivalEngine/Scripts/AI/Actions/FindBestCraftableItem.cs`

## 作用

Find the easiest useful item for the current player to craft and save its item id and missing material ids.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `skipOwnedItems` | `bool` | `true` | When enabled, excludes recipes for items the current player already has in inventory. | - |
| `skipAlreadyCraftedItems` | `bool` | `true` | When enabled, excludes recipes for items the current player has already crafted before. | - |
| `outputOnlyMissingMaterials` | `bool` | `true` | When enabled, outputs only missing material ids. When disabled, outputs all material ids required by the selected recipe. | - |
| `skipRecentlyFailedCraftCandidates` | `bool` | `true` | When enabled, skips craft candidates that recently failed because the tree could not find a gatherable source for their missing materials. | - |
| `requireGatherableMissingMaterials` | `bool` | `true` | When enabled, requires every distinct missing material id to have at least one reachable world item source before the candidate is considered. | - |
| `itemId` | `BBParameter<string>` | - | Output item id of the selected craftable item. Cleared when no suitable recipe is found. | BlackboardOnly |
| `materialItemIds` | `BBParameter<List<string>>` | - | Output material item ids for the selected recipe. Contains missing materials only or all materials based on outputOnlyMissingMaterials. | BlackboardOnly |
| `item` | `ItemData` | - | Item data produced by this craft candidate. | - |
| `missingItemCount` | `int` | - | Total missing exact item material count for this candidate. | - |
| `missingRequirementCount` | `int` | - | Total missing prerequisite craft requirement count for this candidate. | - |
| `missingNearCount` | `int` | - | Set when this candidate requires a nearby crafting station or object that the player is not currently near. | - |
| `totalMaterialCount` | `int` | - | Total number of material ids required by this candidate, including duplicates for required quantities. | - |
| `sortOrder` | `int` | - | Craft sorting order copied from the item data and used as a tie breaker. | - |
| `allMaterialIds` | `List<string>` | `new List<string>()` | All material item ids required by this candidate, including duplicates for required quantities. | - |
| `missingMaterialIds` | `List<string>` | `new List<string>()` | Only the material item ids still missing for this candidate, including duplicates for missing quantities. | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
