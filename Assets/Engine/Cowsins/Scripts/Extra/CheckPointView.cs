/// <summary>
/// This script belongs to cowsins� as a part of the cowsins� FPS Engine. All rights reserved. 
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace cowsins
{
    public class CheckPointView : MonoBehaviour
    {
        public enum MeasureType
        {
            metres, kilometres, inches, feet, yards, miles
        }
        #region variables

        [Tooltip("Attach the text where you want the distance to be displayed"), SerializeField]
        private TextMeshProUGUI text;

        [Tooltip("Select a measure unit among the following"), SerializeField]
        private MeasureType measureType;

        [Tooltip("number of decimals to display"), Range(0, 10), SerializeField]
        private int decimals;

        [Tooltip("How fast you want the text to display the new distance"), SerializeField]
        private float updatePeriod;

        [Tooltip("When enabled, the checkpoint icon and distance text render on top of everything (visible through walls)."), SerializeField]
        private bool seeThrough = true;

        [Tooltip("Maximum distance at which the checkpoint view is visible. Set to 0 or less to always show."), SerializeField]
        private float maxViewDistance = 50f;

        private Transform playerTransform;

        private Canvas canvas;
        #endregion

        private readonly float[] ConversionFactors =
        {
            1f,                  // Metres
            0.001f,              // Kilometres
            39.37f,              // Inches
            3.28084f,            // Feet
            1.09361f,            // Yards
            0.000621371192f      // Miles
        };

        private readonly string[] UnitLabels =
        {
            "m", "km", "inch", "feet", "yards", "miles"
        };

        private void Start()
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            canvas = GetComponentInChildren<Canvas>(true);

            if (seeThrough) MakeSeeThrough();

            StartCoroutine(UpdateValue());
        }

        /// <summary>
        /// Overrides the ZTest on the checkpoint's UI materials so the icon and
        /// distance text are visible through walls/geometry. Both the TMP SDF
        /// shader and the built-in UI/Default shader read the
        /// "unity_GUIZTestMode" property, so we clone each material and set it
        /// to Always (8) on the instance only — leaving global UI unaffected.
        /// </summary>
        private void MakeSeeThrough()
        {
            const float zTestAlways = (float)UnityEngine.Rendering.CompareFunction.Always;

            if (text != null)
            {
                text.fontMaterial = new Material(text.fontSharedMaterial);
                text.fontMaterial.SetFloat("unity_GUIZTestMode", zTestAlways);
            }

            foreach (var img in GetComponentsInChildren<Image>(true))
            {
                if (img.material == null) continue;
                var mat = new Material(img.material);
                mat.SetFloat("unity_GUIZTestMode", zTestAlways);
                img.material = mat;
            }
        }

        /// <summary>
        /// Updates the displayed distance at the specified update period.
        /// </summary>
        private IEnumerator UpdateValue()
        {
            var wait = new WaitForSeconds(updatePeriod);

            while (true)
            {
                UpdateDistanceText();
                yield return wait;
            }
        }

        /// <summary>
        /// Calculates and updates the distance text.
        /// </summary>
        private void UpdateDistanceText()
        {
            if (playerTransform == null) return;

            float baseDistance = Vector3.Distance(transform.position, playerTransform.position);

            // Toggle visibility based on max view distance.
            if (canvas != null)
            {
                bool shouldShow = maxViewDistance <= 0f || baseDistance <= maxViewDistance;
                if (canvas.gameObject.activeSelf != shouldShow)
                    canvas.gameObject.SetActive(shouldShow);
            }

            float convertedDistance = baseDistance * ConversionFactors[(int)measureType];
            string distanceText = convertedDistance.ToString($"F{decimals}") + UnitLabels[(int)measureType];

            if (text != null)
                text.text = distanceText;
        }
    }
}