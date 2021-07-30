using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tiltbrush
{
    public class SkyboxHours : MonoBehaviour
    {
        public Material SkyboxPano;
        public GameObject Sun;
        public TiltBrush.SceneSettings SceneSettings;
        [Range(0f, 24f)]
        [SerializeField] public float Jour;
        public Gradient PaletteJour;
        GradientColorKey[] PaletteJourCouleur;
        GradientAlphaKey[] PaletteJourTransparent;
        float H_Minuit = 24f;
        float H_Midi = 12f;
        float H_Matin = 0f;
        float h_MilieuMatin;
        float h_MilieuApresMidi;
        public Color Matinee;
        public Color Midi;
        public Color Soiree;
        Color Journee;
        string timelog;

        void Start()
        {
            RenderSettings.fogDensity = 1;
            float colorvalue = Random.Range(0f, 1f);
            Matinee = Color.HSVToRGB(colorvalue, 1f, 0.25f);
            Midi = Color.HSVToRGB(colorvalue + 0.072f, 0.6f, 1f);
            Soiree = Color.HSVToRGB(colorvalue + 0.144f, 1f, 0.25f);

            h_MilieuMatin = Random.Range(8f, 11f);
            h_MilieuApresMidi = Random.Range(13f, 17f);

            PaletteJourCouleur = new GradientColorKey[3];
            PaletteJourCouleur[0].color = Matinee;
            PaletteJourCouleur[0].time = 0.0f;
            PaletteJourCouleur[1].color = Midi;
            PaletteJourCouleur[1].time = 0.5f;
            PaletteJourCouleur[2].color = Soiree;
            PaletteJourCouleur[2].time = 1.0f;

            PaletteJourTransparent = new GradientAlphaKey[3];
            PaletteJourTransparent[0].alpha = 1.0f;
            PaletteJourTransparent[0].time = 0.0f;
            PaletteJourTransparent[1].alpha = 1.0f;
            PaletteJourTransparent[1].time = 0.0f;
            PaletteJourTransparent[2].alpha = 1.0f;
            PaletteJourTransparent[2].time = 0.0f;

            PaletteJour.SetKeys(PaletteJourCouleur, PaletteJourTransparent);
        }


        // Update is called once per frame
        void Update()
        {
            RenderSettings.skybox = SkyboxPano;
            timelog = System.DateTime.UtcNow.ToLocalTime().ToString("HH:mm");
            Weather();

        }

        void Weather()
        {
            //Debug.Log(Matinee);
            if (Jour >= H_Matin && Jour <= H_Minuit)
            {
                //Sun.transform.Rotate(360 * (Jour / H_Minuit), 0, 0);
                Sun.transform.eulerAngles = new Vector3(-90 + 360 * (Jour / H_Minuit), 0, 0);
                float t = Mathf.PingPong(Jour / H_Minuit, 1);
                RenderSettings.skybox.SetColor("_Tint", PaletteJour.Evaluate(t));
                RenderSettings.fogColor = PaletteJour.Evaluate(t);
                RenderSettings.skybox.SetFloat("_Exposure", 0.5f);
                DynamicGI.UpdateEnvironment();
            }

            if (Jour == H_Matin)
            {
                RenderSettings.fogDensity = 1;
            }

            if (Jour >= H_Matin && Jour <= h_MilieuMatin)
            {
                RenderSettings.fogDensity = (1f - (Jour/ h_MilieuMatin))/20f;
            }
            else if (Jour >= h_MilieuApresMidi && Jour <= H_Minuit)
            {
                RenderSettings.fogDensity = ((Jour - h_MilieuApresMidi) / (H_Minuit - h_MilieuApresMidi))/20f;
            }

        }

    }
}