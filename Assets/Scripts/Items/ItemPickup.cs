using UnityEngine;

namespace Ashbound
{
    public sealed class ItemPickup : MonoBehaviour
    {
        public Combatant Recipient { get; private set; }
        public ItemDefinition Item { get; private set; }
        public void Configure(Combatant recipient, ItemDefinition item) { Recipient = recipient; Item = item; }
        private void Update()
        {
            if (!Recipient) { Destroy(gameObject); return; }
            if (!Recipient.Combat.CanMove || !Recipient.Alive) return;
            transform.Rotate(0, 60 * Time.deltaTime, 0);
            if (Vector3.Distance(Recipient.transform.position, transform.position) < 1.4f && Recipient.Inventory.TryAdd(Item)) Destroy(gameObject);
        }
    }
}
