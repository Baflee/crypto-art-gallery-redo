using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tiltbrush
{
    public class SkyboxHours : MonoBehaviour
    {
        public Material SkyboxPano;
        public GameObject Sun;
        public Light Moon;
        public TiltBrush.SceneSettings SceneSettings;
        [Range(0f, 86400f)]
        [SerializeField] public float Jour;
        public Gradient PaletteJour;
        GradientColorKey[] PaletteJourCouleur;
        GradientAlphaKey[] PaletteJourTransparent;
        float H_Minuit = 86400f;
        float H_Soir = 64800f;
        float H_Midi = 43200f;
        float H_Mat = 28800f;
        float H_Matin = 0f;
        float H_MilieuMatin;
        float H_MilieuApresMidi;
        public Color Matinee;
        public Color Midi;
        public Color Soiree;
        string timelog;
        float test;

        void Start()
        {
            RenderSettings.fogDensity = 1;
            float colorvalue = Random.Range(0f, 1f);
            Matinee = Color.HSVToRGB(colorvalue, 1f, 0.25f);
            Midi = Color.HSVToRGB(colorvalue + 0.072f, 0.6f, 1f);
            Soiree = Color.HSVToRGB(colorvalue + 0.144f, 1f, 0.25f);

            H_MilieuMatin = Random.Range(28800f, 39600f);
            H_MilieuApresMidi = Random.Range(46800f, 61200f);

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
            string heures = System.DateTime.UtcNow.ToLocalTime().ToString("HH");
            string minutes = System.DateTime.UtcNow.ToLocalTime().ToString("mm");
            string secondes = System.DateTime.UtcNow.ToLocalTime().ToString("ss");
            //Jour = float.Parse(heures) * 3600 + float.Parse(minutes) * 60 + float.Parse(secondes);
            Debug.Log(Jour);
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

            if (Jour >= H_Matin && Jour <= H_MilieuMatin)
            {
                RenderSettings.fogDensity = (1f - (Jour/ H_MilieuMatin))/33f;
            }
            else if (Jour >= H_MilieuApresMidi && Jour <= H_Minuit)
            {
                RenderSettings.fogDensity = ((Jour - H_MilieuApresMidi) / (H_Minuit - H_MilieuApresMidi))/33f;
            }

            if (Jour >= H_Mat && Jour <= H_Midi)
            {
                Moon.intensity = 0.5f + ((Jour - H_Mat) / (H_Midi - H_Mat));
            }
            else if (Jour >= H_Midi && Jour <= H_Soir)
            {
               Moon.intensity = 0.5f + (1f - (Jour / H_Soir));
            }
        }

    }
}