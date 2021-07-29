using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxHours : MonoBehaviour
{
    public Material SkyboxPano;
    [Range(0f, 24f)]
    [SerializeField] public float Jour;
    public Gradient PaletteJour;
    GradientColorKey[] PaletteJourCouleur;
    GradientAlphaKey[] PaletteJourTransparent;
    float H_Minuit = 24f;
    float H_Midi = 12f;
    float H_Matin = 0f;
    public Color Matinee;
    public Color Midi;
    public Color Soiree;
    Color Journee;
    string timelog;

    void Start()
    {
        float colorvalue = Random.Range(0f, 1f);
        Matinee = Color.HSVToRGB(colorvalue, 1f, 0.25f);
        Midi = Color.HSVToRGB(colorvalue + 0.072f, 0.6f, 1f);
        Soiree = Color.HSVToRGB(colorvalue + 0.144f, 1f, 0.25f);

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
            float t = Mathf.PingPong(Jour / H_Minuit, 1);
            RenderSettings.skybox.SetColor("_Tint", PaletteJour.Evaluate(t));
        }

        if (Jour >= H_Matin && Jour <= H_Midi)
        {

        }
        else if (Jour >= H_Midi && Jour < H_Minuit)
        {
            Color.Lerp(Color.red, Color.green, Mathf.PingPong(Jour/H_Minuit, 1));
        }

    }

}
