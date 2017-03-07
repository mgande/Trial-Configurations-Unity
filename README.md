# Trial-Configurations-Unity

    TRIALS_IN_BLOCKS is the number of trials per block. Make sure this is a multiple of 4. Default 40.
    STEPS_FROM_TARGET determines the starting speed of the current trial. Default 4.
    SPEED_STEP_SIZE is the size of each step. Default 5.
    
    Any changes to STEPS_FROM_TARGET and SPEED_STEP_SIZE must still be able to reach the target speed.

    For testing purpose, the trialCounter can be set to any value between 0 and (TRIALS_IN_BLOCKS * 3).
    After testing is complete make sure to reset current_subject to 0 in the JSON.
