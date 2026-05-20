using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public partial class InterfaceInstance
{
    private void WireBuffSlotClicks()
    {
        if (buffSlots == null || buffSlots.Length == 0)
            return;

        for (int i = 0; i < buffSlots.Length; i++)
        {
            var img = buffSlots[i];
            int capturedIndex = i;
            WireImageButton(img, () => OnBuffSlotClicked(capturedIndex));
        }
    }

     private void OnBuffSlotClicked(int index)
     {
         if (buffSlots == null || index < 0 || index >= buffSlots.Length || buffSlots[index] == null)
             return;

         // Chercher le bon Spells basé sur le type de slot (0-2=Support, 3=Healer)
         Spells targetSpells = null;
         if (index < 3 && IsSelectedUnitSupport())
         {
             // Chercher un Support dans la sélection
             targetSpells = FindSpellsOfType(UnitsType.Support);
         }
         else if (index == 3 && IsSelectedUnitHealer())
         {
             // Chercher un Healer dans la sélection
             targetSpells = FindSpellsOfType(UnitsType.Healer);
         }

         if (targetSpells != null)
         {
             targetSpells.ButtonListener(index);
             Debug.Log($"[InterfaceInstance] Bouton buff appuyé: index={index}, nom={buffSlots[index].gameObject.name}", buffSlots[index]);
         }
         else
         {
             Debug.LogWarning($"[InterfaceInstance] Aucun Spells trouvé pour l'index {index}", this);
         }
     }

     private Spells FindSpellsOfType(UnitsType type)
     {
         if (SelectionManager.Instance == null || SelectionManager.Instance.CurrentlySelectedObjects == null)
             return null;

         for (int i = 0; i < SelectionManager.Instance.CurrentlySelectedObjects.Count; i++)
         {
             SelectableObject selected = SelectionManager.Instance.CurrentlySelectedObjects[i];
             if (selected == null)
                 continue;

             UnitInstance unit = selected.GetComponent<UnitInstance>();
             if (unit == null || unit.unitData == null)
                 continue;

             if (unit.unitData.type != type)
                 continue;

             Spells spells = unit.GetComponent<Spells>();
             if (spells != null)
                 return spells;
         }

         return null;
     }

    public void hideBuffIcons()
    {
        SetImageArrayActive(buffSlots, false);
    }

    public void RefreshBuffIconsForSelection()
    {
        hideBuffIcons();

        if (buffSlots == null || buffSlots.Length == 0)
            return;

        if (IsSelectedUnitSupport())
            ShowSupportIcons();

        if (IsSelectedUnitHealer())
            ShowHealerIcon();
    }

    public void ShowSupportIcons()
    {
        if (!IsSelectedUnitSupport())
            return;

        if (buffSlots == null)
            return;

        int slotsToShow = Mathf.Min(buffSlots.Length, 3);
        for (int i = 0; i < slotsToShow; i++)
        {
            if (!IsBuffSlotOnCooldown(i))
                SetImageActive(buffSlots[i], true);
        }
    }

    public void ShowHealerIcon()
    {
        if (!IsSelectedUnitHealer())
            return;

        if (buffSlots == null || buffSlots.Length == 0)
            return;

        int healerIndex = buffSlots.Length - 1;
        if (!IsBuffSlotOnCooldown(healerIndex))
            SetImageActive(buffSlots[healerIndex], true);
    }

    public void HideBuffIconForCooldown(int slotIndex, float cooldown)
    {
        if (buffSlots == null || slotIndex < 0 || slotIndex >= buffSlots.Length)
            return;

        Image slot = buffSlots[slotIndex];
        if (slot == null)
            return;

        if (buffSlotReappearCoroutines.TryGetValue(slotIndex, out var existing) && existing != null)
            StopCoroutine(existing);

        SetImageActive(slot, false);

        if (cooldown <= 0f)
        {
            SetImageActive(slot, true);
            buffSlotReappearCoroutines.Remove(slotIndex);
            return;
        }

        buffSlotReappearCoroutines[slotIndex] = StartCoroutine(ShowBuffIconAfterDelay(slotIndex, cooldown));
    }

    private IEnumerator ShowBuffIconAfterDelay(int slotIndex, float cooldown)
    {
        yield return new WaitForSeconds(cooldown);

        if (buffSlots == null)
        {
            buffSlotReappearCoroutines.Remove(slotIndex);
            yield break;
        }

        if (IsHealerBuffSlot(slotIndex) && !IsSelectedUnitHealer())
        {
            buffSlotReappearCoroutines.Remove(slotIndex);
            yield break;
        }

        if (IsSupportBuffSlot(slotIndex) && !IsSelectedUnitSupport())
        {
            buffSlotReappearCoroutines.Remove(slotIndex);
            yield break;
        }

        if (buffSlots != null && slotIndex >= 0 && slotIndex < buffSlots.Length && buffSlots[slotIndex] != null)
            SetImageActive(buffSlots[slotIndex], true);

        buffSlotReappearCoroutines.Remove(slotIndex);
    }

    private bool IsSelectedUnitSupport()
    {
        return IsSelectedUnitOfType(UnitsType.Support);
    }

    private bool IsSelectedUnitHealer()
    {
        return IsSelectedUnitOfType(UnitsType.Healer);
    }

    private bool IsSelectedUnitOfType(UnitsType type)
    {
        if (SelectionManager.Instance == null || SelectionManager.Instance.CurrentlySelectedObjects == null)
            return false;

        for (int i = 0; i < SelectionManager.Instance.CurrentlySelectedObjects.Count; i++)
        {
            SelectableObject selected = SelectionManager.Instance.CurrentlySelectedObjects[i];
            if (selected == null)
                continue;

            UnitInstance unit = selected.GetComponent<UnitInstance>();
            if (unit == null || unit.unitData == null)
                continue;

            if (unit.unitData.type == type)
                return true;
        }

        return false;
    }

    private bool IsBuffSlotOnCooldown(int slotIndex)
    {
        return buffSlotReappearCoroutines.TryGetValue(slotIndex, out var running) && running != null;
    }

    private bool IsSupportBuffSlot(int slotIndex)
    {
        if (buffSlots == null)
            return false;

        return slotIndex >= 0 && slotIndex < Mathf.Min(3, buffSlots.Length);
    }

    private bool IsHealerBuffSlot(int slotIndex)
    {
        return buffSlots != null && buffSlots.Length > 0 && slotIndex == buffSlots.Length - 1;
    }
}