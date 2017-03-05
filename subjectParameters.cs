using UnityEngine;
using System.IO;
using System.Text;
using System.Collections;

public class subjectParameters : MonoBehaviour {

    private InputParams input;
    private StreamWriter outFile;
    private string filePath;
    private Trial[] trials = new Trial[36];
    private int trialCounter = 0;
    private bool firstTrial = true;
    private Trial currTrial;
    private string columnData = "Subject,Trial,UserHeightView,ManualCalbr,TargetSpeed,SimulateSpeed,Response,SimulatedOrder,Steps,Threshold Speed";
    private Vector3 manualHeightOffset = new Vector3(0, 0, 0);

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
        int counter = 0;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                for (int k = 0; k < 2; k++)
                {
                    for (int l = 0; l < 3; l++)
                    {
                        Trial tmp = new Trial();
                        tmp.targetSpeed = j % 2 == 0 ? input.subject_speed_1[input.current_subject + 1] : input.subject_speed_2[input.current_subject + 1];
                        if (k % 2 == 0)
                        {
                            tmp.increasing = true;
                            tmp.startSpeed = tmp.targetSpeed - (6 * tmp.speedStepSize);
                        } else {
                            tmp.increasing = false;
                            tmp.startSpeed = tmp.targetSpeed + (6 * tmp.speedStepSize);
                        }

                        if (l % 3 == 0)
                        {
                            tmp.heightOffset = -1;
                        } else if (l % 3 == 1)
                        {
                            tmp.heightOffset = 0;
                        } else
                        {
                            tmp.heightOffset = 1;
                        }

                        trials[counter] = tmp;
                        counter++;
                    }
                }
            }
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

    public void setManualHeightOffset(Vector3 vec)
    {
        manualHeightOffset = vec;
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
            return new int[3] { currTrial.startSpeed, currTrial.targetSpeed, currTrial.heightOffset };
        }

        return new int[3] { currTrial.getNextSpeed(), currTrial.targetSpeed, currTrial.heightOffset };
    }

    public void forceQuit()
    {
        writeData(2);
        end();
    }

    private string[] userResponse = new string[] { "lower", "exact", "higher", "quit" };

    private void writeData(int userInput)
    {
        string response = userResponse[userInput + 1];
        string[] values = new string[] {
            (input.current_subject + 1).ToString(),
            trialCounter.ToString() + 1,
            userResponse[currTrial.heightOffset + 1],
            manualHeightOffset.ToString(),
            currTrial.targetSpeed.ToString(),
            currTrial.currSpeed.ToString(),
            response,
            currTrial.increasing ? "increasing" : "decreasing",
            currTrial.steps.ToString()
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
        public int heightOffset;
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