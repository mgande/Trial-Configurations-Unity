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
    private Block[] blocks = new Block[3] { new Block(), new Block(), new Block() };
    private int blockCounter = 0;

    //Parameters that can be tuned
    private static int TRIALS_IN_BLOCKS = 20;
    private static int INITIAL_THRESHOLD = 4;

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
        Subject subject = input.getCurrentSubject();
        if (subject == null)
        {
            return;
        }

        trials = new Trial[TRIALS_IN_BLOCKS * 3];
        int counter = 0;

        //blocks
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < (TRIALS_IN_BLOCKS); j++)
            {
                Trial tmp = new Trial();
                tmp.targetSpeed = j < (TRIALS_IN_BLOCKS / 2) ? subject.speeds[0] : subject.speeds[1];
                tmp.heightOffset = subject.heights[i];
                tmp.increasing = (j % 2) == 0 ? true : false;
                trials[counter] = tmp;
                counter++;
            }
        }
    }

    private void logCurrTrial()
    {
        Debug.Log("Target Speed: " + currTrial.targetSpeed);
        Debug.Log("Current Speed: " + currTrial.speedStepSize);
        Debug.Log("Increasing? : " + currTrial.increasing);
        Debug.Log("Last trial speed: " + currTrial.currSpeed);
    }

    public void setManualHeightOffset(Vector3 vec)
    {
        manualHeightOffset = vec;
    }

    /* Given some user data in {-1, 0, 1} = {lower, equal, higher}
     * @return [currSpeed, targetSpeed, heightOffset]
    */
    public int[] getNextTrial(int userInput)
    {
        if (currTrial == null) {
            currTrial = trials[trialCounter];
            currTrial.init(blocks[blockCounter].threshold, trialCounter);
            return new int[3] { currTrial.currSpeed, currTrial.targetSpeed, currTrial.heightOffset };
        }

        writeData(userInput);

        if ((currTrial.increasing && userInput == 1) || (!currTrial.increasing && userInput == -1)) {
            blocks[blockCounter].updateThreshold(Math.Abs(currTrial.stepsFromTarget));

            if (trialCounter == (TRIALS_IN_BLOCKS * 3)) {
                end();
                return null;
            }

            trialCounter++;
            if (trialCounter >= TRIALS_IN_BLOCKS)
            {
                blockCounter = 1;
            } else if (trialCounter >= (TRIALS_IN_BLOCKS * 2))
            {
                blockCounter = 2;
            }

            currTrial = trials[trialCounter];
            currTrial.init(blocks[blockCounter].threshold, trialCounter);
            return new int[3] { currTrial.currSpeed, currTrial.targetSpeed, currTrial.heightOffset };
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
        public bool increasing;
        public int heightOffset;
        public int speedStepSize = 5;
        public int stepsFromTarget;
        public int steps = 0;
        public int currSpeed;

        public void init(int threshold, int trialCounter)
        {
            if (trialCounter % TRIALS_IN_BLOCKS < 2)
            {
                stepsFromTarget = increasing ? -INITIAL_THRESHOLD : INITIAL_THRESHOLD;
            }
            else
            {
                System.Random rnd = new System.Random();
                int initThreshold = rnd.Next(3, 5) + threshold;
                initThreshold = initThreshold > 4 ? 4 + rnd.Next(-1, 2) : initThreshold;
                stepsFromTarget = increasing ? -initThreshold : initThreshold;
            }
            currSpeed = targetSpeed + (stepsFromTarget * speedStepSize);
        }

        public int getNextSpeed()
        {
            steps++;
        	if (increasing) {
                stepsFromTarget++;
                currSpeed += speedStepSize;
	        } else
            {
                stepsFromTarget--;
                currSpeed -= speedStepSize;
	        }
	        return currSpeed;
        }
    }

    public class Block
    {
        public int threshold = 0;
        private int[] values = new int[20];
        private int count = 0;

        public void updateThreshold(int thresold) {
            values[count] = threshold;
            count++;

            int sum = 0;
            for (int i = 0; i < count; i++)
            {
                sum += values[i];
            }
            threshold = sum / count;
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
