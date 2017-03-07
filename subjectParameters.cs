using UnityEngine;
using System.IO;
using System.Text;
using System.Collections;
using System;

public class subjectParameters : MonoBehaviour {

    private InputParams input;
    private StreamWriter outFile;
    private string filePath;

    private Trial[] trials;
    private int trialCounter = 0;
    private Trial currTrial = null;
    private Vector3 manualHeightOffset = new Vector3(0, 0, 0);
    private string columnData = "Subject,Trial,UserHeightView,ManualCalbr,TargetSpeed,SimulateSpeed,Response,SimulatedOrder,Steps,Threshold Speed";

    //Parameters that can be tuned
    private static int TRIALS_IN_BLOCKS = 40;
    private static int STEPS_FROM_TARGET = 4;
    private static int SPEED_STEP_SIZE = 5;

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
        trials = new Trial[TRIALS_IN_BLOCKS * 3];

        Subject subject = input.getCurrentSubject();
        if (subject == null)
        {
            return;
        }

        //blocks
        for (int i = 0; i < 3; i++)
        {
            int heightOffset = subject.heights[i];

            Trial[] currBlock = new Trial[TRIALS_IN_BLOCKS];
            int counter = 0;
            for (int j = 0; j < (TRIALS_IN_BLOCKS / 4); j++)
            {
                for (int k = 0; k < 2; k++)
                {
                    for (int l = 0; l < 2; l++)
                    {
                        Trial tmp = new Trial();
                        tmp.targetSpeed = subject.speeds[k];
                        tmp.heightOffset = subject.heights[i];
                        tmp.currSpeed = tmp.startSpeed;

                        if (l == 0)
                        {
                            tmp.increasing = true;
                            tmp.startSpeed = tmp.targetSpeed - (STEPS_FROM_TARGET * SPEED_STEP_SIZE);
                        }
                        else
                        {
                            tmp.increasing = false;
                            tmp.startSpeed = tmp.targetSpeed + (STEPS_FROM_TARGET * SPEED_STEP_SIZE);
                        }

                        currBlock[counter] = tmp;
                        counter++;
                    }
                }
            }

            currBlock = randomizeTrials(currBlock);
            trials = appendToTrials(trials, currBlock);
        }
    }

    private Trial[] randomizeTrials(Trial[] trials)
    {
        for (int i = (trials.Length - 1); i > 0; i--)
        {
            int r = UnityEngine.Random.Range(0, i);
            Trial tmp = trials[i];
            trials[i] = trials[r];
            trials[r] = tmp;
        }

        return trials;
    }

    private Trial[] appendToTrials(Trial[] trials1, Trial[] trials2)
    {
        int counter = 0;
        while (trials1[counter] != null)
        {
            counter++;
        }

        for (int i = 0; i < trials2.Length; i++)
        {
            trials1[counter + i] = trials2[i];
        }

        return trials1;
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
        if (currTrial == null) {
            currTrial = trials[trialCounter];
            return new int[3] { currTrial.startSpeed, currTrial.targetSpeed, currTrial.heightOffset };
        }

        writeData(userInput);

        if ((currTrial.increasing && userInput == 1) || (!currTrial.increasing && userInput == -1)) {
            if (trialCounter == (TRIALS_IN_BLOCKS * 3)) {
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

    public class Trial
    {
        public int targetSpeed;
        public int startSpeed;
        public bool increasing;
        public int heightOffset;
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

    [Serializable]
    public class Subject
    {
        public int[] speeds;
        public int[] heights;
    }

    [Serializable]
    public class InputParams
    {
        public int current_subject;
        public Subject[] subjects;

        public Subject getCurrentSubject()
        {
            if (subjects == null)
            {
                return null;
            }
            return subjects[current_subject % (subjects.Length)];
        }
    }
}