using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

public class EarthquakeManager : MonoBehaviour
{
    public Camera mainCamera;
    public TMP_Text alertText;
    public GameObject alertSign;
    public GameObject earthquakeIndicator;
    public List<Building> buildings;

    private string jsonFilePath;
    private string logFilePath;

    void Start()
    {
        jsonFilePath = Path.Combine(Application.streamingAssetsPath, "earthquake_data.json");
        logFilePath = Path.Combine(Application.persistentDataPath, "earthquake_log.txt");

        StartCoroutine(CheckForEarthquakeData());
    }

    IEnumerator CheckForEarthquakeData()
    {
        while (true)
        {
            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                EarthquakeData data = JsonUtility.FromJson<EarthquakeData>(json);

                if (data != null)
                {
                    LogEvent($"[INFO] Earthquake Detected - Magnitude: {data.magnitude}, P-Wave: {data.p_wave.arrival_time}, S-Wave: {data.s_wave.arrival_time}");

                    // Delay before P-Wave
                    yield return new WaitForSeconds(6.0f);

                    // P-Wave simulation with reduced intensity
                    alertText.text = "P-Wave Detected: Minor shaking expected!";
                    yield return StartCoroutine(ShakeCamera(0.01f, 5.0f)); // Greatly reduced intensity
                    LogEvent("[EVENT] P-Wave simulated.");

                    // Predictive analysis before S-wave hits
                    PredictiveDisplacementAnalysis(data.magnitude);

                    // Delay after predictive analysis and before S-Wave
                    yield return new WaitForSeconds(12.0f);

                    // S-Wave simulation
                    alertText.text = "S-Wave Detected: Strong shaking incoming!";
                    yield return StartCoroutine(ShakeCamera(1.0f, 3.0f)); // Stronger shaking
                    LogEvent("[EVENT] S-Wave simulated.");

                    // Analyze structural response post S-wave
                    AnalyzeBuildingDisplacement(data.magnitude);

                    alertText.text = "Earthquake Over.";
                    LogEvent("[END] Earthquake simulation complete.");
                }
            }

            yield return new WaitForSeconds(5);
        }
    }

    IEnumerator ShakeCamera(float intensity, float duration)
    {
        Vector3 originalPosition = mainCamera.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float xOffset = Random.Range(-intensity, intensity) * 0.1f;
            float yOffset = Random.Range(-intensity, intensity) * 0.1f;
            mainCamera.transform.position = originalPosition + new Vector3(xOffset, yOffset, 0);

            if (earthquakeIndicator != null)
            {
                earthquakeIndicator.transform.localScale = Vector3.one * (1.0f + Random.Range(-0.1f, 0.1f));
                earthquakeIndicator.transform.position = new Vector3(xOffset, yOffset, 0);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = originalPosition;
        if (earthquakeIndicator != null)
            earthquakeIndicator.transform.localScale = Vector3.one;
    }

    void PredictiveDisplacementAnalysis(float magnitude)
    {
        foreach (Building building in buildings)
        {
            float displacement = CalculateDisplacement(building.elasticModulus, building.momentOfInertia, building.height, building.floors, magnitude);
            string cautionLevel = GetCautionLevel(displacement);

            LogEvent($"[PREDICTIVE] Building: {building.buildingObject.name}, Predicted Displacement: {displacement:F3}m, Risk: {cautionLevel}");

            if (cautionLevel == "High")
            {
                alertText.text = $"WARNING: {building.buildingObject.name} at HIGH risk before S-wave!";
                alertSign.SetActive(true);
            }
        }
    }

    void AnalyzeBuildingDisplacement(float magnitude)
    {
        foreach (Building building in buildings)
        {
            float displacement = CalculateDisplacement(building.elasticModulus, building.momentOfInertia, building.height, building.floors, magnitude);
            string cautionLevel = GetCautionLevel(displacement);

            LogEvent($"[AFTERSHOCK] Building: {building.buildingObject.name}, Actual Displacement: {displacement:F3}m, Risk: {cautionLevel}");

            if (cautionLevel == "High")
            {
                alertText.text = $"Severe Damage Risk: {building.buildingObject.name}";
                building.buildingObject.GetComponent<Renderer>().material.color = Color.red;
                StartCoroutine(TiltBuilding(building.buildingObject, 10.0f + building.floors));
            }
            else if (cautionLevel == "Medium")
            {
                alertText.text = $"Moderate Risk: {building.buildingObject.name}";
                building.buildingObject.GetComponent<Renderer>().material.color = Color.yellow;
                StartCoroutine(TiltBuilding(building.buildingObject, 5.0f + (building.floors * 0.5f)));
            }
            else
            {
                alertText.text = $"{building.buildingObject.name} is stable.";
                building.buildingObject.GetComponent<Renderer>().material.color = Color.green;
            }
        }

        alertSign.SetActive(false);
    }

    float CalculateDisplacement(float E, float I, float L, int floors, float magnitude)
    {
        float force = magnitude * 100000 * (1 + (floors * 0.1f));
        float displacement = (force * Mathf.Pow(L, 3)) / (3 * E * I);
        return displacement * (1 + (floors * 0.05f));
    }

    string GetCautionLevel(float displacement)
    {
        if (displacement > 0.5f)
            return "High";
        else if (displacement > 0.2f)
            return "Medium";
        else
            return "Low";
    }

    IEnumerator TiltBuilding(GameObject building, float tiltAngle)
    {
        float elapsedTime = 0;
        float duration = 1.0f;

        while (elapsedTime < duration)
        {
            float angle = Mathf.Lerp(0, tiltAngle, elapsedTime / duration);
            building.transform.rotation = Quaternion.Euler(angle, 0, 0);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(1.0f);

        elapsedTime = 0;
        while (elapsedTime < duration)
        {
            float angle = Mathf.Lerp(tiltAngle, 0, elapsedTime / duration);
            building.transform.rotation = Quaternion.Euler(angle, 0, 0);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    void LogEvent(string message)
    {
        string logLine = $"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
        File.AppendAllText(logFilePath, logLine + "\n");
        Debug.Log(logLine);
    }
}

[System.Serializable]
public class EarthquakeData
{
    public float magnitude;
    public PWave p_wave;
    public SWave s_wave;
}

[System.Serializable]
public class PWave { public float arrival_time; }
[System.Serializable]
public class SWave { public float arrival_time; }

[System.Serializable]
public class Building
{
    public GameObject buildingObject;
    public float elasticModulus;
    public float momentOfInertia;
    public float height;
    public int floors;
}
