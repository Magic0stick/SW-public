using System.Linq;
using System.Numerics;
using System.Data;
using Content.Server.Chat.Systems;
using Content.Server.Cult.Components;
using Content.Server.Imperial.Medieval.Cult.Bloodspells.Materials;
using Content.Server.Imperial.Medieval.Cult.Bloodspells.Light;
using Content.Server.Hands.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Timing;
using System.Linq;
using System.Numerics;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Hands.Systems;
using Content.Server.Imperial.Medieval.Cult.Bloodspells.light;
using Content.Shared.Alert;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Imperial.Medieval.Cult;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.Medieval.Cult.Bloodspells;

public sealed class CultCastSystem : EntitySystem
{
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly AlertsSystem _alert = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;

    private const int MaxStoredMessages = 3;
    private const float MinTimeBetweenWords = 0.7f;
    private const float MaxSpellCastTime = 5.0f;
    private const float MaterialLookupRange = 1f;
    private const float DeathCurseDamageMultiplier = 1.5f;
    private const int DeathCurseCountDivisor = 2;

    private List<BloodSpellPrototype> _bloodSpells = new();
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CultMemberComponent, EntitySpokeEvent>(OnSpoke);
        SubscribeLocalEvent<CultMemberComponent, DamageChangedEvent>(OnDamageReceived);
        SubscribeLocalEvent<CultMemberComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CultMemberComponent, AttackAttemptEvent>(OnAttackAttempt);

    }

    private void OnInit(EntityUid uid, CultMemberComponent component, ComponentInit args)
    {
        if (component.DeathCurse)
            _alert.ShowAlert(uid, component.DeathCurseAlert);
    }

    private void OnDamageReceived(EntityUid uid, CultMemberComponent component, DamageChangedEvent args)
    {
        if (!component.DeathCurse || !args.DamageIncreased || !args.Origin.HasValue)
            return;

        var attacker = args.Origin.Value;

        if (HasComp<CultMemberComponent>(attacker))
            return;

        if (TryComp<CultCursedComponent>(attacker, out var cursed) && cursed.CurseLevel != 0)
            return;

        ApplyOrIncreaseCurse(attacker);
        RemoveDeathCurse(uid, component);
    }

    private void OnAttackAttempt(EntityUid uid, CultMemberComponent component, AttackAttemptEvent args)
    {
        if (!component.DeathCurse || args.Target == null)
            return;

        var target = args.Target.Value;

        if (HasComp<CultMemberComponent>(target))
            return;

        if (TryComp<CultCursedComponent>(target, out var cursed) && cursed.CurseLevel != 0)
            return;

        RemoveDeathCurse(uid, component);
    }

    private void ApplyOrIncreaseCurse(EntityUid target)
    {
        if (TryComp<DeathCurseComponent>(target, out var existingCurse))
        {
            foreach (var key in existingCurse.CurseDamage.DamageDict.Keys.ToList())
            {
                existingCurse.CurseDamage.DamageDict[key] *= DeathCurseDamageMultiplier;
                existingCurse.CurseCount /= DeathCurseCountDivisor;
            }
        }
        else
        {
            EnsureComp<DeathCurseComponent>(target);
        }
    }

    private void RemoveDeathCurse(EntityUid uid, CultMemberComponent component)
    {
        component.DeathCurse = false;
        _alert.ClearAlert(uid, component.DeathCurseAlert);
    }

    private bool CheckSpellSequence(Queue<(string message, TimeSpan time)> queue, BloodSpellPrototype spell)
    {
        var sequenceLength = spell.Incantation.Count;
        if (queue.Count < sequenceLength)
            return false;

        var recentMessages = queue.Reverse().Take(sequenceLength).Reverse().Select(item => item.message).ToArray();

        return recentMessages.Zip(spell.Incantation, (msg, spellWord) => 
            msg.Equals(spellWord, StringComparison.OrdinalIgnoreCase)).All(match => match);
    }

    private bool ValidateSpellTiming(Queue<(string message, TimeSpan time)> messages, EntityUid caster)
    {
        if (messages.Count == 0)
            return false;

        var messageArray = messages.ToArray();

        for (int i = 1; i < messageArray.Length; i++)
        {
            var timeDiff = messageArray[i].time - messageArray[i - 1].time;
            if (timeDiff.TotalSeconds < MinTimeBetweenWords)
            {
                _popupSystem.PopupEntity(Loc.GetString("cult-spell-too-fast"), caster, caster);
                return false;
            }
        }

        var oldestMessage = messages.MaxBy(item => item.time);
        if (oldestMessage.time.TotalSeconds <= MaxSpellCastTime)
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-spell-too-slow"), caster, caster);
            return false;
        }

        return true;
    }

    private void OnSpoke(EntityUid uid, CultMemberComponent component, EntitySpokeEvent args)
    private bool TryBlood(EntityUid uid, float count)
    {
        if (!TryComp<SolutionContainerManagerComponent>(uid, out var solutionComp))
            return false;


        if (!_solution.TryGetSolution(uid, "bloodstream", out var bloodSolution))
            return false;

        if (bloodSolution.Value.Comp.Solution.Volume <
            bloodSolution.Value.Comp.Solution.MaxVolume * count)
        {
            _popupSystem.PopupEntity("У тебя не достаточно крови", uid, uid);
            return false;
        }

        _solution.RemoveEachReagent((uid, bloodSolution),
                        bloodSolution.Value.Comp.Solution.MaxVolume * count);
        return true;
    }

    private bool CheckTime(Queue<(string message, TimeSpan time)> lastSpokenMessages , EntityUid uid)
    {
        if (component.LastSpokenMessages.Count >= MaxStoredMessages)
            component.LastSpokenMessages.Dequeue();

        component.LastSpokenMessages.Enqueue((args.Message, _timing.CurTime));

        if (!TryComp<BloodstreamComponent>(uid, out var bloodstream) || bloodstream.BleedAmount == 0)
            return;

        var spells = _prototypeManager
            .EnumeratePrototypes<BloodSpellPrototype>()
            .OrderByDescending(s => s.Incantation.Count);

        foreach (var spell in spells)
        {
            if (spell.Incantation.Count == 0 || spell.Incantation.Last() != args.Message)
                continue;

            if (!CheckSpellSequence(component.LastSpokenMessages, spell))
                continue;

            if (!ValidateSpellTiming(component.LastSpokenMessages, uid))
                return;

            ExecuteSpell(uid, component, spell);
            return;
        }
    }

    private void ExecuteSpell(EntityUid caster, CultMemberComponent component, BloodSpellPrototype spell)
    {
        switch (spell.SpellType)
        {
            case BloodSpellType.ItemUpgrade:
                CastItemUpgradeSpell(caster, spell);
                break;

            case BloodSpellType.CraftItem:
                CastCraftingSpell(caster, spell);
                break;

            case BloodSpellType.CraftArmor:
                CastCraftingSpell(caster, spell, isArmor: true);
                break;

            case BloodSpellType.DeathCurse:
                CastDeathCurseSpell(caster, component, spell);
                break;

            default:
                _popupSystem.PopupEntity(Loc.GetString("cult-spell-incorrect"), caster, caster);
                break;
        }
    }

    private void CastItemUpgradeSpell(EntityUid caster, BloodSpellPrototype spell)
    {
        if (!_handsSystem.TryGetActiveItem(caster, out var heldItem))
        {
            var failMsg = spell.FailureMessage ?? "cult-spell-need-holy-scripture";
            _popupSystem.PopupEntity(Loc.GetString(failMsg), caster, caster);
            return;
        }

        var itemPrototype = _entityManager.GetComponent<MetaDataComponent>(heldItem.Value).EntityPrototype?.ID;
        if (spell.RequiredHeldItem == null || itemPrototype != spell.RequiredHeldItem.Value.Id)
        {
            var itemName = _entityManager.GetComponent<MetaDataComponent>(heldItem.Value).EntityPrototype?.Name ?? "unknown";
            _popupSystem.PopupEntity(Loc.GetString("cult-spell-wrong-item", ("item", itemName)), caster, caster);
            return;
        }

        if (spell.ReplacementItem == null)
            return;

        EntityManager.DeleteEntity(heldItem.Value);
        var newItem = Spawn(spell.ReplacementItem.Value, Transform(caster).Coordinates);
        _handsSystem.TryPickup(caster, newItem, checkActionBlocker: false);

        if (spell.SuccessMessage != null)
            _popupSystem.PopupEntity(Loc.GetString(spell.SuccessMessage), caster, caster);
    }

    private void CastCraftingSpell(EntityUid caster, BloodSpellPrototype spell, bool isArmor = false)
    {
        if (spell.RequiredMaterial == null || spell.SpawnProto == null)
            return;

        var (availableCount, materials) = CollectNearbyMaterials(caster, spell.RequiredMaterial, spell.RequiredMaterialCount);
        if (availableCount < spell.RequiredMaterialCount)
        {
            var failMsg = spell.FailureMessage ?? "cult-spell-insufficient-materials";
            _popupSystem.PopupEntity(Loc.GetString(failMsg), caster, caster);
            return;
        }

        if (isArmor && spell.ReplaceEquipment)
            {"Ave", "The", "Truth"},
            {"Ave", "The", "Bronus"},
            {"Ave", "The", "Vilkus"},
            {"Ave", "The", "Knatus"},
            {"Ave", "The", "Sekir"},
            {"Ave", "The", "Magical"},
            {"Katariemai", "Opoion", "Me chtypisei"},
            {"Bringt", "Mi", "Zuruk"},
            // {"Дебагус", "Магикус", "Призывус"}
            // {"Elderberry", "Fig", "Banana"}
        };

        for (int i = bloodcasts.GetLength(0)-1; i >= 0; i--)
        {
            if (spell.EquipmentSlot == null ||
                !TryComp<InventoryComponent>(caster, out var inventory) || 
                !_inventorySystem.TryGetSlotEntity(caster, spell.EquipmentSlot, out var existingOutfit))
            {
                var failMsg = spell.FailureMessage ?? "cult-spell-insufficient-materials";
                _popupSystem.PopupEntity(Loc.GetString(failMsg), caster, caster);
                return;
            }

            ConsumeMaterials(materials);
            EntityManager.DeleteEntity(existingOutfit.Value);
            var armor = Spawn(spell.SpawnProto.Value, Transform(caster).Coordinates);
            _inventorySystem.TryEquip(caster, armor, spell.EquipmentSlot, silent: true, force: true, inventory: inventory);
        }
        else
        {
            ConsumeMaterials(materials);
            Spawn(spell.SpawnProto.Value, Transform(caster).Coordinates);
        }

        if (spell.SuccessMessage != null)
            _popupSystem.PopupEntity(Loc.GetString(spell.SuccessMessage), caster, caster);
    }

    private (int totalCount, List<(EntityUid entity, int takeCount)> materials) CollectNearbyMaterials(
        EntityUid caster,
        string materialType,
        int requiredCount)
    {
        var result = new List<(EntityUid entity, int takeCount)>();
        int foundTotal = 0;

        var entities = _lookup.GetEntitiesInRange(caster, MaterialLookupRange)
            .Where(e => TryComp<BloodMaterialComponent>(e, out var mat) && mat.MaterialType == materialType)
            .ToList();

        var sortedEntities = entities.OrderByDescending(e => {
            int count = TryComp<StackComponent>(e, out var stack) ? stack.Count : 1;
            return count >= requiredCount ? int.MaxValue : count; 
        }).ThenByDescending(e => _stack.GetCount(e));

        foreach (var entity in sortedEntities)
        {
            if (foundTotal >= requiredCount)
                break;

            int availableInStack = _stack.GetCount(entity);
            int neededFromThisStack = Math.Min(availableInStack, requiredCount - foundTotal);

            result.Add((entity, neededFromThisStack));
            foundTotal += neededFromThisStack;
        }

        return (foundTotal, result);
    }

    private void ConsumeMaterials(List<(EntityUid entity, int takeCount)> materials)
    {
        foreach (var (entity, takeCount) in materials)
        {
            if (!TryComp<StackComponent>(entity, out var stack))
            {
                EntityManager.DeleteEntity(entity);
                continue;
            }

            if (stack.Count <= takeCount)
            {
                EntityManager.DeleteEntity(entity);
            }
            else
            {
                _stack.SetCount(entity, stack.Count - takeCount, stack);
                case "Призывус":
                {
                    var center = Transform(uid).Coordinates;
                    for (int x = -4; x <= 5; x++)
                    {
                        for (int y = -4; y <= 5; y++)
                        {
                            var b = Spawn("MedievalCultBrushFine", center.Offset(new Vector2(x*0.5f, y*-0.5f)));
                            if (!TryComp<CultBloodPaintComponent>(b, out var bloodPaint))
                                break;
                            bloodPaint.PosX = x+5;
                            bloodPaint.PosY = y+5;
                        }
                    }
                    break;
                }


               case "Truth":
                {
                    if (_handsSystem.TryGetActiveItem(uid, out var heldItem))
                    {
                        // Проверяем, что предмет в руке — это именно книга-прототип (MedievalBookCultGuide)
                        if (!_entityManager.GetComponent<MetaDataComponent>(heldItem.Value).EntityPrototype?.ID.Equals("MedievalBookCultGuide") == true)
                        {
                            _popupSystem.PopupEntity("В руке должно быть святое писание! а не " + _entityManager.GetComponent<MetaDataComponent>(heldItem.Value).EntityPrototype?.Name, uid, uid);
                            break;
                        }

                        // Удаляем текущий предмет в руке
                        _entityManager.DeleteEntity(heldItem.Value);

                        // Спавним новую книгу и экипируем её
                        var newBook = Spawn("MedievalBookCultGuide2", Transform(uid).Coordinates);
                        _handsSystem.TryPickup(uid, newBook, checkActionBlocker: false);
                    }
                    else
                    {
                        _popupSystem.PopupEntity("В руке должно быть святое писание!", uid, uid);

                    }
                    break;
                }


                case "Bronus":
                {
                    var needCount = 5;
                    var myMaterials = new List<EntityUid>{};
                    foreach (var target in _lookup.GetEntitiesInRange(uid, 1f))
                    {
                        if (TryComp<BloodMaterialComponent>(target, out var bloodMaterial) && bloodMaterial.MaterialType == "BloodIron")
                        {
                            myMaterials.Add(target);
                            needCount--;
                            if (needCount == 0)
                            {
                                if (TryComp<InventoryComponent>(uid, out var inventory) && _inventorySystem.TryGetSlotEntity(uid, "outerClothing", out var existingOutfit))
                                {
                                    foreach (var material in myMaterials)
                                    {
                                        _entityManager.DeleteEntity(material);
                                    }
                                    _entityManager.DeleteEntity(existingOutfit.Value);
                                    var b = Spawn("MedievalClothingOuterArmorCultUp", Transform(uid).Coordinates);
                                    _inventorySystem.TryEquip(uid, b, "outerClothing", silent: true, force: true, inventory: inventory);
                                    return;
                                }

                            }
                        }
                    }
                    _popupSystem.PopupEntity("Ресурсов не достаточно", uid, uid);
                    break;
                }
                case "Vilkus":
                {
                    var needCount = 5;
                    var myMaterials = new List<EntityUid>{};
                    foreach (var target in _lookup.GetEntitiesInRange(uid, 1f))
                    {
                        if (TryComp<BloodMaterialComponent>(target, out var bloodMaterial) && bloodMaterial.MaterialType == "BloodIron")
                        {
                            myMaterials.Add(target);
                            needCount--;
                            if (needCount == 0)
                            {
                                foreach (var material in myMaterials)
                                {
                                    _entityManager.DeleteEntity(material);
                                }
                                Spawn("MedievalSpearCult", Transform(uid).Coordinates);
                                return;
                            }
                        }
                    }
                    _popupSystem.PopupEntity("Ресурсов не достаточно", uid, uid);
                    break;
                }
                case "Knatus":
                {
                    var needCount = 5;
                    var myMaterials = new List<EntityUid>{};
                    foreach (var target in _lookup.GetEntitiesInRange(uid, 1f))
                    {
                        if (TryComp<BloodMaterialComponent>(target, out var bloodMaterial) && bloodMaterial.MaterialType == "BloodIron")
                        {
                            myMaterials.Add(target);
                            needCount--;
                            if (needCount == 0)
                            {
                                foreach (var material in myMaterials)
                                {
                                    _entityManager.DeleteEntity(material);
                                }
                                Spawn("MedievalCultYatagan", Transform(uid).Coordinates);
                                return;
                            }
                        }
                    }
                    _popupSystem.PopupEntity("Ресурсов не достаточно", uid, uid);
                    break;
                }
                case "Sekir":
                {
                    var needCount = 5;
                    var myMaterials = new List<EntityUid>{};
                    foreach (var target in _lookup.GetEntitiesInRange(uid, 1f))
                    {
                        if (TryComp<BloodMaterialComponent>(target, out var bloodMaterial) && bloodMaterial.MaterialType == "BloodIron")
                        {
                            myMaterials.Add(target);
                            needCount--;
                            if (needCount == 0)
                            {
                                foreach (var material in myMaterials)
                                {
                                    _entityManager.DeleteEntity(material);
                                }
                                Spawn("MedievalIronSekirCult", Transform(uid).Coordinates);
                                return;
                            }
                        }
                    }
                    _popupSystem.PopupEntity("Ресурсов не достаточно", uid, uid);
                    break;
                }
                case "Magical":
                {
                    var needCount = 5;
                    var myMaterials = new List<EntityUid>{};
                    foreach (var target in _lookup.GetEntitiesInRange(uid, 1f))
                    {
                        if (TryComp<BloodMaterialComponent>(target, out var bloodMaterial) && bloodMaterial.MaterialType == "BloodLeather")
                        {
                            myMaterials.Add(target);
                            needCount--;
                            if (needCount == 0)
                            {
                                if (TryComp<InventoryComponent>(uid, out var inventory) && _inventorySystem.TryGetSlotEntity(uid, "outerClothing", out var existingOutfit))
                                {
                                    foreach (var material in myMaterials)
                                    {
                                        _entityManager.DeleteEntity(material);
                                    }
                                    _entityManager.DeleteEntity(existingOutfit.Value);
                                    var b = Spawn("MedievalClothingOuterArmorCultMana", Transform(uid).Coordinates);
                                    _inventorySystem.TryEquip(uid, b, "outerClothing", silent: true, force: true, inventory: inventory);
                                    return;
                                }

                            }
                        }
                    }
                    _popupSystem.PopupEntity("Ресурсов не достаточно", uid, uid);
                    break;
                }
                case "Me chtypisei":
                {
                    if (!component.DeathCusre)
                    {
                        component.DeathCusre = true;
                        _popupSystem.PopupEntity("Да будет проклят тот, кто меня ударит", uid, uid);
                        _alert.ShowAlert(uid, component.DeathCurseAlert);
                    }
                    else
                    {
                        _popupSystem.PopupEntity("Ты чуствуешь, что проклятье сильно", uid, uid);
                    }
                    break;
                }
                case "Zuruk":
                {
                    if (!TryBlood(uid, 0.6f))
                        return;
                    var txform = Transform(uid);
                    var tcoords = txform.Coordinates;
                    Spawn("MedievalTeleportZuruk", tcoords);
                    var ouraltar = EnsureComp<CultTeleportedComponent>(uid).Portal;
                    if (TryComp<CultTeleportComponent>(ouraltar, out var teleported) && teleported.Base)
                        _transform.SetCoordinates(uid, Transform(ouraltar).Coordinates);
                    else
                    {
                        _popupSystem.PopupEntity("Ты чуствуешь что тебе некуда вернутся, надо телепортироватся куда то сначало.", uid, uid);
                    }
                    break;
                }
                default:
                    _popupSystem.PopupEntity("Ты чуствуешь неправильность в своих словах", uid, uid);
                    break;
            }
        }
    }

    private void CastDeathCurseSpell(EntityUid caster, CultMemberComponent component, BloodSpellPrototype spell)
    {
        if (!component.DeathCurse)
        {
            component.DeathCurse = true;
            var successMsg = spell.SuccessMessage ?? "cult-spell-death-curse-activated";
            _popupSystem.PopupEntity(Loc.GetString(successMsg), caster, caster);
            _alert.ShowAlert(caster, component.DeathCurseAlert);
        }
        else
        {
            var failMsg = spell.FailureMessage ?? "cult-spell-death-curse-already-active";
            _popupSystem.PopupEntity(Loc.GetString(failMsg), caster, caster);
        }
    }
}
