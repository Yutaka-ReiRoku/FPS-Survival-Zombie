using UnityEngine;

namespace cowsins
{
    public class CollectibleHighlight : MonoBehaviour
    {
        private Outline outline;
        private IntelligenceSkillSystem intelligence;

        private void Start()
        {
            outline = GetComponent<Outline>();

            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                intelligence =
                    player.GetComponent<IntelligenceSkillSystem>();
            }

            if (outline != null)
            {
                outline.enabled = false;
            }
        }

        private void Update()
        {
            if (outline == null ||
                intelligence == null)
                return;

            outline.enabled =
                intelligence.HighlightCollectibles;
        }
    }
}