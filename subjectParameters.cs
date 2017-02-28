using UnityEngine;
using System.IO;
using System.Text;
using System.Collections;

public class subjectParameters : MonoBehaviour {

    private InputParams input;
    private StreamWriter outFile;
    private Trial[] trials = new Trial[40];
    private int trialCounter = 0;
    private int currTrialCounter = 0;
    private Trial currTrial;
    private string filePath;
    private int speedStepSize = 5;

    /**
		Black box implmentation.
		Main thread asks for currect speed and gives response (-1, 0, 1).
    **/

    // Use this for initialization
    void Awake () {
		print ("start called");
        filePath = Application.dataPath + "/Scripts";
        string lineJson = "";
        string currLine;
        StreamReader reader = new StreamReader(filePath + "/input.json", Encoding.Default);
        using (reader)
        {
            do
            {
                currLine = reader.ReadLine();
                lineJson += currLine;
            }
            while (currLine != null);
        }

        input = JsonUtility.FromJson<InputParams>(lineJson);

        string filename = filePath + "/subjectData" + input.current_subject + ".csv";
        outFile = File.CreateText(filename);
        setTrials();
		Debug.Log (trials [0]);

    }

    private void setTrials()
    {
        for (int i = 0; i < 40; i++)
        {
            Trial tmp = new Trial();
            tmp.speed = i % 2 == 0 ? input.subject_speed_1[input.current_subject + 1] : input.subject_speed_2[input.current_subject + 1];

            int group = (int) Mathf.Floor(i / 10);
            if (group == 0 || group == 2)
            {
                tmp.increasing = false;
                tmp.startSpeed = tmp.speed + (6 * speedStepSize);
            } else {
            	tmp.startSpeed = tmp.speed - (6 * speedStepSize);
            }

            trials[i] = tmp;
        }

        // Fisher-Yates Shuffle
        for (int i = (trials.Length - 1); i > 0; i--)
        {
            int r = Random.Range(0, i);
            Trial tmp = trials[i];
            trials[i] = trials[r];
            trials[r] = tmp;
        }

    }

    private void logCurrTrial()
    {
        Debug.Log("Speed: " + currTrial.speed);
        Debug.Log("Start Speed: " + currTrial.startSpeed);
        Debug.Log("Increasing? : " + currTrial.increasing);
    }

    /* userInput = -1 : Lower
    *  userInput = 0 : Equal
    *  userInput = 1 : Higher
    */
    public int[] getNextTrial(int userInput)
    {
		//return new int[] { 20, 30 };
        if (trialCounter == 0 && currTrialCounter == 0) {
            currTrial = trials[trialCounter];
            currTrialCounter++;
			Debug.Log (trials[0]);
            // logCurrTrial();
            return new int[2]{ currTrial.startSpeed, currTrial.speed };
        }

        // logCurrTrial();
        currTrial.outData += userInput + ",";

        if ((currTrial.increasing && userInput == 1) || (!currTrial.increasing && userInput == -1)) {
            writeData();

            if (trialCounter == 40) {
                end();
                return null;
            }

            currTrial = trials[trialCounter];
            trialCounter++;
            currTrialCounter = 0;
            return new int[2] { currTrial.startSpeed, currTrial.speed };
        }

        currTrialCounter++;

        if (currTrial.increasing) {
            return new int[2] { currTrial.startSpeed + ((currTrialCounter - 1) * speedStepSize), currTrial.speed };
        } else {
            return new int[2] { currTrial.startSpeed - ((currTrialCounter - 1) * speedStepSize), currTrial.speed };
        }

    }

    public void forceQuit()
    {
        writeData();
        end();
    }

    private void writeData()
    {
        string str = currTrial.speed + ",";
        str += currTrial.increasing ? "increaseing," : "decreasing,";
        outFile.WriteLine(str + currTrial.outData);
    }

    private void end()
    {
        outFile.Close();

        input.current_subject += 1;
        StreamWriter jsonOut = File.CreateText(filePath + "/input.json");
        jsonOut.Write(JsonUtility.ToJson(input));
        jsonOut.Close();
    }


    //private bool once = false;
	// Update is called once per frame
	//void Update () {
    //}

    private class InputParams
    {
        public float threshold_min;
        public float threshold_max;
        public int current_subject = 0;
        public int[] subject_speed_1;
        public int[] subject_speed_2;
    }

    public class Trial
    {
        public int speed;
        public int startSpeed;
        public bool increasing = true;
        public float heightOffset;
        public string outData = "";
    }
}