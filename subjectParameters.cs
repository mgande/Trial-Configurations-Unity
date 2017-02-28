using UnityEngine;
using System.IO;
using System.Text;
using System.Collections;

public class subjectParameters : MonoBehaviour {

    private InputParams input;
    private StreamWriter outFile;
    private string filePath;
    private Trial[] trials = new Trial[40];
    private int trialCounter = 0;
    private bool firstTrial = true;
    private Trial currTrial;
    private string columnData = "Subject,Trial,UserHeightView,ManualCalbr,TargetSpeed,SimulateSpeed,Response,SimulatedOrder,Steps,Threshold Speed";

    /**
		Black box implmentation.
		Main thread asks for currect speed and gives response (-1, 0, 1).
    **/

    // Use this for initialization
    void Start () {
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
        outFile.WriteLine(columnData);
        setTrials();
    }

    private void setTrials()
    {
        for (int i = 0; i < 40; i++)
        {
            Trial tmp = new Trial();
            tmp.targetSpeed = i % 2 == 0 ? input.subject_speed_1[input.current_subject + 1] : input.subject_speed_2[input.current_subject + 1];

            int group = (int) Mathf.Floor(i / 10);
            if (group == 0 || group == 2)
            {
                tmp.increasing = false;
                tmp.startSpeed = tmp.targetSpeed + (6 * tmp.speedStepSize);
            } else {
            	tmp.startSpeed = tmp.targetSpeed - (6 * tmp.speedStepSize);
            }
            tmp.currSpeed = tmp.startSpeed;

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
        Debug.Log("Target Speed: " + currTrial.targetSpeed);
        Debug.Log("Start Speed: " + currTrial.startSpeed);
        Debug.Log("Increasing? : " + currTrial.increasing);
        Debug.Log("Last trial speed: " + currTrial.currSpeed);
    }

    /* Given some user data in {-1, 0, 1} = {lower, equal, higher}
     * @return [startSpeed, currTrialSpeed]
    */
    public int[] getNextTrial(int userInput)
    {
		//return new int[] { 20, 30 };
        if (firstTrial) {
            firstTrial = false;
            currTrial = trials[trialCounter];
            return new int[2]{ currTrial.startSpeed, currTrial.targetSpeed };
        }

        // logCurrTrial();
        writeData(userInput);

        if ((currTrial.increasing && userInput == 1) || (!currTrial.increasing && userInput == -1)) {
            if (trialCounter == 40) {
                end();
                return null;
            }

            trialCounter++;
            currTrial = trials[trialCounter];
            return new int[2] { currTrial.startSpeed, currTrial.targetSpeed };
        }

        return new int[2] { currTrial.getNextSpeed(), currTrial.targetSpeed };
    }

    public void forceQuit()
    {
        writeData(2);
        end();
    }

    private string[] userResposne = new string[] { "lower", "exact", "higher", "quit" };

    private void writeData(int userInput)
    {
        string response = userResposne[userInput + 1];
        string[] values = new string[] {
            (input.current_subject + 1).ToString(),
            trialCounter.ToString() + 1,
            "N/A",
            "N/A",
            currTrial.targetSpeed.ToString(),
            currTrial.currSpeed.ToString(),
            response,
            currTrial.increasing ? "increasing" : "decreasing",
            currTrial.steps.ToString(),
            "N/A"
        };

        string strOut = "";
        for (int i = 0; i < values.Length; i++)
        {
            strOut += values[i] + ",";
        }
        outFile.WriteLine(strOut.Substring(0, strOut.Length - 1));
    }

    private void end()
    {
        outFile.Close();

        input.current_subject += 1;
        StreamWriter jsonOut = File.CreateText(filePath + "/input.json");
        jsonOut.Write(JsonUtility.ToJson(input));
        jsonOut.Close();
    }


    private bool once = true;
	void Update () {
		if (once) {
            getNextTrial(0);
            for (int i = 0; i < 10; i++)
            {
                getNextTrial(Random.Range(-1, 2));
            }

            forceQuit();

			once = false;
		}		
    }

    private class InputParams
    {
        public int current_subject;
        public int[] subject_speed_1;
        public int[] subject_speed_2;
    }

    public class Trial
    {
        public int targetSpeed;
        public int startSpeed;
        public bool increasing = true;
        public float heightOffset;
        public string outData = "";
        public int speedStepSize = 5;
        public int steps = 0;
        public int currSpeed;

        public int getNextSpeed()
        {
        	steps++;
        	if (increasing) {
        		currSpeed += speedStepSize;
	        } else {
	        	currSpeed -= speedStepSize;
	        }
	        return currSpeed;
        }
    }
}