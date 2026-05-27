using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public partial class InterfaceInstance
{
    /// <summary>
    /// Attache les callbacks de clic aux emplacements de buff disponibles dans l'UI.
    /// </summary>
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

     /// <summary>
     /// Gère le clic sur un emplacement de buff : recherche le spells approprié et déclenche son listener.
     /// </summary>
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

      /// <summary>
      /// Recherche parmi la sélection d'unités un composant Spells correspondant au type demandé.
      /// </summary>
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

     /// <summary>
     /// Masque toutes les icônes de buff dans l'UI.
     /// </summary>
     public void hideBuffIcons()
     {
        SetImageArrayActive(buffSlots, false);
    }

     /// <summary>
     /// Actualise l'affichage des icônes de buff en fonction de l'unité(s) sélectionnée(s).
     /// </summary>
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

     /// <summary>
     /// Affiche les icônes de support si l'unité sélectionnée possède des capacités de support.
     /// </summary>
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

     /// <summary>
     /// Affiche l'icône de healer si l'unité sélectionnée possède des capacités de soin.
     /// </summary>
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

     /// <summary>
     /// Cache une icône de buff pendant la durée de cooldown puis la réaffiche quand le cooldown est écoulé.
     /// </summary>
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

     /// <summary>
     /// Coroutine qui attend le cooldown puis réaffiche l'icône de buff si les conditions sont toujours valides.
     /// </summary>
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

     /// <summary>
     /// Indique si la sélection courante contient au moins une unité de type Support.
     /// </summary>
     private bool IsSelectedUnitSupport()
     {
        return IsSelectedUnitOfType(UnitsType.Support);
    }

     /// <summary>
     /// Indique si la sélection courante contient au moins une unité de type Healer.
     /// </summary>
     private bool IsSelectedUnitHealer()
     {
        return IsSelectedUnitOfType(UnitsType.Healer);
    }

     /// <summary>
     /// Vérifie si la sélection contient une unité d'un type donné.
     /// </summary>
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

     /// <summary>
     /// Vérifie si le slot de buff est actuellement en cooldown (coroutine en cours).
     /// </summary>
     private bool IsBuffSlotOnCooldown(int slotIndex)
     {
        return buffSlotReappearCoroutines.TryGetValue(slotIndex, out var running) && running != null;
    }

     /// <summary>
     /// Indique si l'index correspond à un slot de support (les premiers slots).
     /// </summary>
     private bool IsSupportBuffSlot(int slotIndex)
     {
        if (buffSlots == null)
            return false;

        return slotIndex >= 0 && slotIndex < Mathf.Min(3, buffSlots.Length);
    }

     /// <summary>
     /// Indique si l'index correspond au slot dédié au healer (dernier slot).
     /// </summary>
     private bool IsHealerBuffSlot(int slotIndex)
     {
        return buffSlots != null && buffSlots.Length > 0 && slotIndex == buffSlots.Length - 1;
    }
}