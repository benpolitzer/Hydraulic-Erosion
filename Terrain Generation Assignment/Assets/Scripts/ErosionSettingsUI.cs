using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ErosionSettingsUI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private HydraulicErosion hydraulicErosion;

    [Header("Sliders")]
    [SerializeField] private Slider erosionStrengthSlider;
    [SerializeField] private Slider depositAmountSlider;
    [SerializeField] private Slider waterAmountSlider;
    [SerializeField] private Slider dropletCountSlider;
    [SerializeField] private Slider flowLengthSlider;
    [SerializeField] private Slider simulationSpeedSlider;

    [Header("Buttons")]
    [SerializeField] private Button pauseResumeButton;
    [SerializeField] private Button stepSimulationButton;
    [SerializeField] private Button showPathsButton;
    [SerializeField] private Button resetButton;

    [Header("Optional Button Text")]
    [SerializeField] private TMP_Text pauseResumeButtonText;
    [SerializeField] private TMP_Text showPathsButtonText;

    [Header("Camera")]
    [SerializeField] private CamMovement camMovement;
    [SerializeField] private Button resetCameraButton;

    private void Start()
    {
        if (hydraulicErosion == null)
        {
            Debug.LogError("ErosionSettingsUI is missing a HydraulicErosion reference.");
            return;
        }

        SetupSliders();
        SetupButtons();

        UpdatePauseButtonText();
        UpdatePathsButtonText();
    }

    private void SetupSliders()
    {
        SetupSlider(
            erosionStrengthSlider,
            0.001f,
            0.08f,
            hydraulicErosion.erosionRate,
            false,
            hydraulicErosion.SetErosionRate
        );

        SetupSlider(
            depositAmountSlider,
            0.001f,
            0.08f,
            hydraulicErosion.depositionRate,
            false,
            hydraulicErosion.SetDepositionRate
        );

        SetupSlider(
            waterAmountSlider,
            0.1f,
            5f,
            hydraulicErosion.initialWaterAmount,
            false,
            hydraulicErosion.SetInitialWaterAmount
        );

        SetupSlider(
            dropletCountSlider,
            100f,
            3000f,
            hydraulicErosion.particleCount,
            true,
            hydraulicErosion.SetParticleCount
        );

        SetupSlider(
            flowLengthSlider,
            5f,
            200f,
            hydraulicErosion.maxParticleSteps,
            true,
            hydraulicErosion.SetMaxParticleSteps
        );

        SetupSlider(
            simulationSpeedSlider,
            0f,
            10f,
            hydraulicErosion.erosionIterationsPerFrame,
            true,
            hydraulicErosion.SetErosionIterationsPerFrame
        );
    }

    private void SetupSlider(
        Slider slider,
        float minValue,
        float maxValue,
        float startingValue,
        bool wholeNumbers,
        UnityEngine.Events.UnityAction<float> onChanged)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.wholeNumbers = wholeNumbers;
        slider.value = startingValue;

        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(onChanged);
    }

    private void SetupButtons()
    {
        if (pauseResumeButton != null)
        {
            pauseResumeButton.onClick.RemoveAllListeners();
            pauseResumeButton.onClick.AddListener(OnPauseResumePressed);
        }

        if (stepSimulationButton != null)
        {
            stepSimulationButton.onClick.RemoveAllListeners();
            stepSimulationButton.onClick.AddListener(hydraulicErosion.StepSimulation);
        }

        if (showPathsButton != null)
        {
            showPathsButton.onClick.RemoveAllListeners();
            showPathsButton.onClick.AddListener(OnShowPathsPressed);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(hydraulicErosion.ResetErosion);
        }
        if (resetCameraButton != null && camMovement != null)
        {
            resetCameraButton.onClick.RemoveAllListeners();
            resetCameraButton.onClick.AddListener(camMovement.ResetCameraPosition);
        }
    }

    private void OnPauseResumePressed()
    {
        hydraulicErosion.TogglePauseSimulation();
        UpdatePauseButtonText();
    }

    private void OnShowPathsPressed()
    {
        hydraulicErosion.ToggleFlowLines();
        UpdatePathsButtonText();
    }

    private void UpdatePauseButtonText()
    {
        if (pauseResumeButtonText == null)
        {
            return;
        }

        if (hydraulicErosion.IsSimulationPaused)
        {
            pauseResumeButtonText.text = "Resume";
        }
        else
        {
            pauseResumeButtonText.text = "Pause";
        }
    }

    private void UpdatePathsButtonText()
    {
        if (showPathsButtonText == null)
        {
            return;
        }

        if (hydraulicErosion.AreFlowLinesVisible)
        {
            showPathsButtonText.text = "Hide Paths";
        }
        else
        {
            showPathsButtonText.text = "Show Paths";
        }
    }
}