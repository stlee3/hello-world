using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;

namespace CCEApplication.StateMachine
{
    public class RFIDStateMachine
    {
        #region Vairable
        private bool bThreadExit = false;
        private bool m_bIsStateRunning = false;
        private bool m_b1stBranchToStateEntry = false;
        private bool m_bInitialise = false;
        private bool m_bReturnFromState = false;

        private bool m_bEStoppressing = false;
        private bool m_bFaultDetected = false;
        private bool m_bEStopPressed = false;

        private bool m_bFrontDoorFault = false;
        private bool m_b1stTimeSequenceEntry = false;
        private bool m_bCameraStationInitialiseDone = false;

        private int m_nXAxisPressed = 0;
        private int m_nYAxisPressed = 0;

        private int nSequence = 99;
        private int m_nSequence = 99;
        private int m_nBranchPrevious = 99;
        private int m_nStateNoPrevious = 99;
        private int m_nStateNoCurrent = 99;

        private Machine m_pStation;
        private EventState m_pEventPausing;
        private enumActivity[] eMainStateMode = new enumActivity[100];
        private Thread RunThread;


        private bool m_bCheckFinishTime = false;
        private DateTime m_StartCheckFinishTime;
        private DateTime m_EndCheckFinishTime;
        private TimeSpan m_FinishTime;
        private DigitalIOInterface m_pIOInterface;

        #endregion

        #region enum
        enum enumActivity
        {
            eInitStation = 0,
            eUninitialise,
            eInitialise,
            eFault,
            eEStop,
            eReference,
            ePausing,
            ePaused,
            eStopping,
            eStopped,
            eRunning,
            eProduction,
            eIsCassettePresent,
            eReadRF,
            eIsRemoveCassette,
            eCompleted,
            eReferenceCompleted,
            eEndSequence,
        }

        bool m_bProduction = false;
        private enumActivity[] eProductionMode = new enumActivity[100];
        #endregion

        public RFIDStateMachine(Machine pStationOwner, DigitalIOInterface IOInterface)
        {
            m_pStation = pStationOwner;
            m_pIOInterface = IOInterface;
            m_pEventPausing = new EventState();

            DoResetSequence();
            eProductionMode[nSequence++] = enumActivity.eIsCassettePresent;
            eProductionMode[nSequence++] = enumActivity.eReadRF;
            eProductionMode[nSequence++] = enumActivity.eIsRemoveCassette;
            eProductionMode[nSequence++] = enumActivity.eCompleted;
            DoResetSequence();

            RunThread = new Thread(Run);
            RunThread.Start();
        }

        ~RFIDStateMachine()
        {
            bThreadExit = true;
            m_pEventPausing = null;
            RunThread.Abort();
        }

        private void Run()
        {
            try
            {
                while (true)
                {
                    OnExecute();

                    if (bThreadExit)
                        break;
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                m_pStation.SetFaultDetect(true);
                MessageBox.Show("Main State Machine Thread Exit!!!. Please restart program.\n" + ex.ToString());

            }

        }

        private void DoResetSequence()
        {
            nSequence = 0;
            m_nSequence = nSequence;
        }

        private void DoSetNextSequence(int n = 1)
        {
            nSequence += n;
            m_nSequence = nSequence;
        }

        private void DoSetPreviousSequence(int n = 1)
        {
            nSequence -= n;
            m_nSequence = nSequence;
        }

        private void DoSequence(enumActivity eSequence)
        {
            switch (eSequence)
            {
                case enumActivity.eInitStation:
                    // m_pStation.SetUpdateState("eInitCameraStation");
                    CallState(enumActivity.eInitStation);
                    break;
                case enumActivity.eReferenceCompleted:
                    //m_pStation.SetUpdateState("eReferenceCompleted");
                    CallState(enumActivity.eReferenceCompleted);
                    break;

                case enumActivity.eIsCassettePresent:
                    CallState(enumActivity.eIsCassettePresent);
                    break;
                case enumActivity.eReadRF:
                    CallState(enumActivity.eReadRF);
                    break;
                case enumActivity.eIsRemoveCassette:
                    CallState(enumActivity.eIsRemoveCassette);
                    break;
                case enumActivity.eCompleted:
                    CallState(enumActivity.eCompleted);
                    break;
                default:
                    MessageBox.Show("RFID State Machine DoSequence ERROR. Please restart program.");
                    break;
            }
        }

        private void CallState(enumActivity eStateNo)
        {
            if ((m_nStateNoPrevious != (int)eStateNo) && !m_b1stTimeSequenceEntry)
            {
                m_b1stTimeSequenceEntry = true;
                m_nStateNoCurrent = (int)eStateNo;
                m_bReturnFromState = false;

                switch (eStateNo)
                {
                    case enumActivity.eInitStation:
                        OnEnterInitStation();
                        break;
                    case enumActivity.eReferenceCompleted:
                        OnEnterReferenceCompleted();
                        break;
                    case enumActivity.eUninitialise:
                        OnEnterUninitialise();
                        break;
                    case enumActivity.eInitialise:
                        OnEnterInitialise();
                        break;
                    case enumActivity.eFault:
                        OnEnterFault();
                        break;
                    case enumActivity.eEStop:
                        OnEnterEStop();
                        break;
                    case enumActivity.eReference:
                        OnEnterReference();
                        break;
                    case enumActivity.ePausing:
                        OnEnterPausing();
                        break;
                    case enumActivity.ePaused:
                        OnEnterPaused();
                        break;
                    case enumActivity.eStopping:
                        OnEnterStopping();
                        break;
                    case enumActivity.eStopped:
                        OnEnterStopped();
                        break;
                    case enumActivity.eRunning:
                        OnEnterRunning();
                        break;
                    case enumActivity.eIsCassettePresent:
                        OnEnterIsCassettePresent();
                        break;
                    case enumActivity.eReadRF:
                        OnEnterReadRF();
                        break;
                    case enumActivity.eIsRemoveCassette:
                        OnEnterIsRemoveCassette();
                        break;
                    case enumActivity.eCompleted:
                        OnEnterCompleted();
                        break;
                    default:
                        MessageBox.Show("Main State Machine Call State ERROR. Please restart program.");
                        break;
                }
            }

            if ((m_nStateNoCurrent == (int)eStateNo) && m_b1stTimeSequenceEntry)
            {

                switch (eStateNo)
                {
                    case enumActivity.eInitStation:
                        OnExecuteInitStation();
                        if (m_bReturnFromState)
                        {
                            m_nStateNoPrevious = m_nStateNoCurrent;
                            m_b1stTimeSequenceEntry = false;
                            m_bReturnFromState = false;
                        }
                        break;
                    case enumActivity.eReferenceCompleted:
                        OnExecuteReferenceCompleted();
                        if (m_bReturnFromState)
                        {
                            m_nStateNoPrevious = m_nStateNoCurrent;
                            m_b1stTimeSequenceEntry = false;
                            m_bReturnFromState = false;
                        }
                        break;
                    /*	case eEndSequence:
                            OnExecuteEndSequence();
                            if(m_bReturnFromState)
                            {
                              m_nStateNoPrevious = m_nStateNoCurrent;
                              m_b1stTimeSequenceEntry = false;
                              m_bReturnFromState = false;
                            }
                            break;
                    */
                    case enumActivity.eUninitialise:
                        OnExecuteUninitialise();
                        if (m_bReturnFromState)
                        {
                            m_nStateNoPrevious = m_nStateNoCurrent;
                            m_b1stTimeSequenceEntry = false;
                            m_bReturnFromState = false;
                        }
                        break;
                    case enumActivity.eInitialise:
                        OnExecuteInitialise();
                        if (m_bReturnFromState)
                        {
                            m_nStateNoPrevious = m_nStateNoCurrent;
                            m_b1stTimeSequenceEntry = false;
                            m_bReturnFromState = false;
                        }
                        break;
                    case enumActivity.eFault:
                        OnExecuteFault();
                        if (m_bReturnFromState)
                        {
                            m_nStateNoPrevious = m_nStateNoCurrent;
                            m_b1stTimeSequenceEntry = false;
                            m_bReturnFromState = false;
                        }
                        break;
                    case enumActivity.eEStop:
                        OnExecuteEStop();
                        if (m_bReturnFromState)
                        {
                            m_nStateNoPrevious = m_nStateNoCurrent;
                            m_b1stTimeSequenceEntry = false;
                            m_bReturnFromState = false;
                        }
                        break;
                    case enumActivity.eReference:
                        OnExecuteReference();
                        if (m_bReturnFromState)
                        {
                            m_nStateNoPrevious = m_nStateNoCurrent;
                            m_b1stTimeSequenceEntry = false;
                            m_bReturnFromState = false;
                        }
                        break;
                    case enumActivity.ePausing:
                        OnExecutePausing();
                        if (m_bReturnFromState)
                        {
                            m_nStateNoPrevious = m_nStateNoCurrent;
                            m_b1stTimeSequenceEntry = false;
                            m_bReturnFromState = false;
                        }

                        break;
                    case enumActivity.ePaused:
                        OnExecutePaused();
                        if (m_bReturnFromState)
                        {
                            m_nStateNoPrevious = m_nStateNoCurrent;
                            m_b1stTimeSequenceEntry = false;
                            m_bReturnFromState = false;
                        }
                        break;
                    case enumActivity.eStopping:
                        OnExecuteStopping();
                        if (m_bReturnFromState)
                        {
                            m_nStateNoPrevious = m_nStateNoCurrent;
                            m_b1stTimeSequenceEntry = false;
                            m_bReturnFromState = false;
                        }
                        break;
                    case enumActivity.eStopped:
                        OnExecuteStopped();
                        if (m_bReturnFromState)
                        {
                            m_nStateNoPrevious = m_nStateNoCurrent;
                            m_b1stTimeSequenceEntry = false;
                            m_bReturnFromState = false;
                        }

                        break;
                    case enumActivity.eRunning:
                        OnExecuteRunning();
                        if (m_bReturnFromState)
                        {
                            m_nStateNoPrevious = m_nStateNoCurrent;
                            m_b1stTimeSequenceEntry = false;
                            m_bReturnFromState = false;
                        }
                        break;
                    case enumActivity.eIsCassettePresent:
                        OnExecuteIsCassettePresent();
                        if (m_bReturnFromState)
                        {
                            m_nStateNoPrevious = m_nStateNoCurrent;
                            m_b1stTimeSequenceEntry = false;
                            m_bReturnFromState = false;
                        }
                        break;
                    case enumActivity.eReadRF:
                        OnExecuteReadRF();
                        if (m_bReturnFromState)
                        {
                            m_nStateNoPrevious = m_nStateNoCurrent;
                            m_b1stTimeSequenceEntry = false;
                            m_bReturnFromState = false;
                        }
                        break;
                    case enumActivity.eIsRemoveCassette:
                        OnExecuteIsRemoveCassette();
                        if (m_bReturnFromState)
                        {
                            m_nStateNoPrevious = m_nStateNoCurrent;
                            m_b1stTimeSequenceEntry = false;
                            m_bReturnFromState = false;
                        }
                        break;
                    case enumActivity.eCompleted:
                        OnExecuteCompleted();
                        if (m_bReturnFromState)
                        {
                            m_nStateNoPrevious = m_nStateNoCurrent;
                            m_b1stTimeSequenceEntry = false;
                            m_bReturnFromState = false;
                        }
                        break;
                    default:
                        MessageBox.Show("Main State Machine Call State ERROR. Please restart program.");
                        break;
                }
            }
        }

        public bool IsFaultDetected()
        {
            return m_bFaultDetected;
        }

        private void Reset()
        {
            m_bInitialise = false;
        }

        public bool IsInitialise()
        {
            return (m_bInitialise == true);
        }

        public void SetThreadExit()
        {
            bThreadExit = true;
        }

        private void ReturnFromState()
        {
            m_bReturnFromState = true;
        }

        private bool IsStateRunning()// start Running
        {
            bool bStatus = false;

            if (m_pStation.IsEStopButtonActive())
            {
                if (!m_pStation.IsInStartMode() || (m_pStation.IsInStartMode() && !m_bIsStateRunning))
                {
                    if (IsStartButtonPress() && m_pStation.GetStartEventState() != GlobalVariable.eEventSetting.eSetInProgress)
                    {
                        //m_pStation.SetSoftResumeRunningEven(false);

                        m_pStation.SetStartMode(true);
                        bStatus = true;


                    }
                    else if ((m_pStation.GetStartEventState() == GlobalVariable.eEventSetting.eSetCompleted) && !m_pStation.IsFaultReset())
                        bStatus = true;
                }
            }
            return bStatus;
        }

        private bool IsStartButtonPress()
        {
            bool bStatus = false;
            int nCount = 0;
            //if (m_pStation.IsStartButtonActive() || SingleLockEventFunct("UI2ThreadSoftStart"))
            if (m_pStation.IsStartButtonActive())
            {
                //ResetEventFunct("UI2ThreadSoftStart");

                while (true)
                {
                    Thread.Sleep(10);
                    //if (!m_pStation.IsStartButtonActive() || SingleLockEventFunct("UI2ThreadSoftStart"))
                    if (!m_pStation.IsStartButtonActive())
                    {
                        //ResetEventFunct("UI2ThreadSoftStart"); //original
                        bStatus = true;
                        break;
                    }
                    nCount++;
                    if (nCount > 200) // cm ooi
                        break;
                }
            }
            return bStatus;
        }

        private bool IsBreakButtonPress()
        {
            bool bStatus = false;
            int nCountTimeOut = 0;

            if (m_pStation.IsBreakButtonActive())
            {
                while (true)
                {

                    Thread.Sleep(10);
                    if (!m_pStation.IsBreakButtonActive())
                    {
                        bStatus = true;
                        break;
                    }
                    nCountTimeOut++;
                    if (nCountTimeOut > 20)
                        break;
                }
            }
            return bStatus;
        }

        private bool IsSpeedUpButtonPress()
        {
            bool bStatus = false;
            return bStatus;
        }

        private bool IsSpeedDownButtonPress()
        {
            bool bStatus = false;
            return bStatus;
        }

        private bool IsInspectButtonPress()
        {
            bool bStatus = false;


            return bStatus;
        }

        private bool IsStateReference()
        {
            bool bStatus = false;

            if (m_pStation.IsEStopButtonActive())
            {
                //if (SingleLockEventFunct("UI2ThreadSoftInitialize") && !m_pStation.IsInStartMode() && !IsFaultDetected() && !IsFaultOccur())
                if (!m_pStation.IsInStartMode() && !IsFaultDetected() && !IsFaultOccur())
                {
                    //ResetEventFunct("UI2ThreadSoftInitialize");
                    if (!m_pStation.IsInResetMode())
                    {
                        // ResetEventFunct("UI2ThreadSoftInitialize");
                        bStatus = true;
                    }
                }
                else if (m_pStation.IsInResetMode())
                    bStatus = true;
            }
            return bStatus;
        }

        private bool IsStatePause()
        {
            bool bStatus = false;

            if ((m_pStation.IsInPauseMode()/* || IsStartButtonPress()*/) && m_pStation.IsInStartMode() && (!(m_pStation.GetPauseEventState() == GlobalVariable.eEventSetting.eSetInProgress) && !(m_pStation.GetPausingEventState() == GlobalVariable.eEventSetting.eSetInProgress)))
            {
                //	NxGlobal::m_pStationInfo->SetPauseTiming();
                m_pStation.SetPausingMode(true);
                m_pStation.SetPauseMode(true);
                m_pStation.SetPausingInProgressMode(true);
                bStatus = true;
            }
            else if (m_pStation.IsInPausingMode() || m_pStation.GetPauseEventState() == GlobalVariable.eEventSetting.eSetInProgress || m_pStation.GetPausingEventState() == GlobalVariable.eEventSetting.eSetInProgress)
                bStatus = true;

            return bStatus;
        }

        private bool IsStateCycleStop()
        {
            bool bStatus = false;

            if ((m_pStation.IsInPauseMode() || IsBreakButtonPress()) && m_pStation.IsInStartMode() &&
                (!(m_pStation.GetPauseEventState() == GlobalVariable.eEventSetting.eSetInProgress) && !(m_pStation.GetPausingEventState() == GlobalVariable.eEventSetting.eSetInProgress)))
            {
                m_pStation.SetPausingMode(true);
                bStatus = true;
            }
            else if (m_pStation.GetPauseEventState() == GlobalVariable.eEventSetting.eSetInProgress || m_pStation.GetPausingEventState() == GlobalVariable.eEventSetting.eSetInProgress)
                bStatus = true;

            return bStatus;
        }

        private bool IsFaultOccur()
        {
            bool bStatus = false;

            if (m_pStation.IsFaultDetect())
                bStatus = true;

            return bStatus;
        }

        private void IsStateBreak()
        {
            if (IsBreakButtonPress())
            {
                if (!m_pStation.IsInBreakMode())
                {
                    m_pStation.SetBreakMode(true);
                    m_pIOInterface.OnDOChangeState(GlobalVariable.DigitalOutputList.O_BREAK_LAMP, true); // Break lamp
                }
                else
                {
                    m_pStation.SetBreakMode(false);
                    m_pIOInterface.OnDOChangeState(GlobalVariable.DigitalOutputList.O_BREAK_LAMP, false);
                }
            }
        }

        private void OnExecute()
        {
            //IsStateBreak();

            BranchToState(enumActivity.eProduction);//OnEnterProduction();

            /*
                        if (!m_pStation.IsEStopButtonActive() || m_bEStoppressing)
                        {
                            m_bIsStateRunning = false;
                            BranchToState(enumActivity.eEStop);
                        }

                        // Check Main Air Pressure Low
                        if (m_pStation.IsMainAirPressureSwitchActive())
                        {
                            GlobalVariable.nAlarmNo = 1;
                            GlobalVariable.nAlarmStation = 1;
                            m_bFaultDetected = true;
                            BranchToState(enumActivity.eFault);
                        }

                        // check fault command
                        // any station feedback to main state machine, it will go into fault state
                        if (m_pStation.FaultOccur() && m_pStation.IsEStopButtonActive())
                        {
                            m_bIsStateRunning = false;
                            BranchToState(enumActivity.eFault);
                        }

                        if (IsInitialise() && m_pStation.IsCompletedSystemResetMode() && m_pStation.IsEStopButtonActive())
                        {                
                            if (!m_pStation.IsFaultDetect() && IsStateRunning())// start
                            {
                                m_bIsStateRunning = true;
                                BranchToState(enumActivity.eRunning);
                            }
                        }

                        if (IsStatePause() && m_pStation.IsEStopButtonActive())// stop 
                        {
                            m_bIsStateRunning = false;
                            BranchToState(enumActivity.ePaused);
                        }
                        if (IsStateCycleStop() && m_pStation.IsEStopButtonActive())
                        {
                            m_bIsStateRunning = false;
                            BranchToState(enumActivity.ePaused);
                        }
                        if (IsStateReference() && m_pStation.IsEStopButtonActive())// do system home
                        {
                            m_bIsStateRunning = true;
                            BranchToState(enumActivity.eReference);
                        }
                        if (IsFaultOccur() && m_pStation.IsEStopButtonActive())
                        {
                            m_bIsStateRunning = false;
                            BranchToState(enumActivity.eFault);
                        }
             */
        }

        private void BranchToState(enumActivity eStateNo)
        {
            if (m_b1stBranchToStateEntry)
            {
                if (m_nBranchPrevious != (int)eStateNo)
                {
                    m_b1stBranchToStateEntry = false;
                }
            }

            BranchToStateProcess(eStateNo);
        }

        private void BranchToStateProcess(enumActivity eStateNo)
        {
            switch (eStateNo)
            {
                case enumActivity.eProduction:
                    if (!m_b1stBranchToStateEntry)
                    {
                        m_nBranchPrevious = (int)eStateNo;
                        m_b1stBranchToStateEntry = true;
                        OnEnterProduction();
                    }
                    else if (m_b1stBranchToStateEntry)
                        OnExecuteProduction();
                    break;

                case enumActivity.eUninitialise:
                    if (!m_b1stBranchToStateEntry)
                    {
                        m_nBranchPrevious = (int)eStateNo;
                        m_b1stBranchToStateEntry = true;
                        m_pStation.SetUpdateState("EnterUninitialise");
                        OnEnterUninitialise();
                    }
                    else if (m_b1stBranchToStateEntry)
                    {
                        m_pStation.SetUpdateState("ExeUninitialise");
                        OnExecuteUninitialise();
                    }
                    break;
                case enumActivity.eInitialise:
                    if (!m_b1stBranchToStateEntry)
                    {
                        m_nBranchPrevious = (int)eStateNo;
                        m_b1stBranchToStateEntry = true;
                        m_pStation.SetUpdateState("EnterInitialise");
                        OnEnterInitialise();
                    }
                    else if (m_b1stBranchToStateEntry)
                    {
                        m_pStation.SetUpdateState("ExeInitialise");
                        OnExecuteInitialise();
                    }
                    break;
                case enumActivity.eFault:
                    if (!m_b1stBranchToStateEntry)
                    {
                        m_nBranchPrevious = (int)eStateNo;
                        m_b1stBranchToStateEntry = true;
                        m_pStation.SetUpdateState("EnterFault");
                        OnEnterFault();
                    }
                    else if (m_b1stBranchToStateEntry)
                    {
                        m_pStation.SetUpdateState("ExeFault");
                        OnExecuteFault();
                    }
                    break;
                case enumActivity.eEStop:
                    if (!m_b1stBranchToStateEntry)
                    {
                        m_nBranchPrevious = (int)eStateNo;
                        m_b1stBranchToStateEntry = true;
                        m_pStation.SetUpdateState("EnterEStop");
                        OnEnterEStop();
                    }
                    else if (m_b1stBranchToStateEntry)
                    {
                        m_pStation.SetUpdateState("ExeEStop");
                        OnExecuteEStop();
                    }
                    break;
                case enumActivity.eReference:
                    if (!m_b1stBranchToStateEntry)
                    {
                        m_nBranchPrevious = (int)eStateNo;
                        m_b1stBranchToStateEntry = true;
                        m_pStation.SetUpdateState("EnterReference");
                        OnEnterReference();
                    }
                    if (m_b1stBranchToStateEntry)
                    {
                        m_pStation.SetUpdateState("ExeReference");
                        OnExecuteReference();
                    }
                    break;
                case enumActivity.ePaused:
                    if (!m_b1stBranchToStateEntry)
                    {
                        m_nBranchPrevious = (int)eStateNo;
                        m_b1stBranchToStateEntry = true;
                        m_pStation.SetUpdateState("EnterPaused");
                        OnEnterPaused();
                    }
                    if (m_b1stBranchToStateEntry)
                    {
                        m_pStation.SetUpdateState("ExePaused");
                        OnExecutePaused();
                    }
                    break;
                case enumActivity.ePausing:
                    if (!m_b1stBranchToStateEntry)
                    {
                        m_nBranchPrevious = (int)eStateNo;
                        m_b1stBranchToStateEntry = true;
                        m_pStation.SetUpdateState("EnterPausing");
                        OnEnterPausing();
                    }
                    if (m_b1stBranchToStateEntry)
                    {
                        m_pStation.SetUpdateState("ExePausing");
                        OnExecutePausing();
                    }
                    break;
                case enumActivity.eRunning:
                    if (!m_b1stBranchToStateEntry)
                    {
                        m_nBranchPrevious = (int)eStateNo;
                        m_b1stBranchToStateEntry = true;
                        m_pStation.SetUpdateState("EnterRunning");
                        OnEnterRunning();
                    }
                    if (m_b1stBranchToStateEntry)
                    {
                        m_pStation.SetUpdateState("ExeRunning");
                        OnExecuteRunning();
                    }
                    break;
                case enumActivity.eStopped:
                    if (!m_b1stBranchToStateEntry)
                    {
                        m_nBranchPrevious = (int)eStateNo;
                        m_b1stBranchToStateEntry = true;
                        m_pStation.SetUpdateState("EnterStopped");
                        OnEnterStopped();
                    }
                    if (m_b1stBranchToStateEntry)
                    {
                        m_pStation.SetUpdateState("ExeStopped");
                        OnExecuteStopped();
                    }
                    break;
                case enumActivity.eStopping:
                    if (!m_b1stBranchToStateEntry)
                    {
                        m_nBranchPrevious = (int)eStateNo;
                        m_b1stBranchToStateEntry = true;
                        m_pStation.SetUpdateState("EnterStopping");
                        OnEnterStopping();
                    }
                    if (m_b1stBranchToStateEntry)
                    {
                        m_pStation.SetUpdateState("ExeStopping");
                        OnExecuteStopping();
                    }
                    break;
                default:
                    MessageBox.Show("RFID State Machine BranchToStateProcess ERROR. Please restart program.");
                    break;
            }
        }

        //////////////////////////////////////////////////////////////////////////////
        ///////////////////////////  Init State   ////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////
        private void OnEnterInitStation()
        {

        }
        private void OnExecuteInitStation()
        {
        }

        private void OnEnterReferenceCompleted()
        {
        }
        private void OnExecuteReferenceCompleted()
        {
            Thread.Sleep(300);
            DoSetNextSequence();
            ReturnFromState();
        }

        private void OnEnterUninitialise()
        {
            m_pStation.SetStopMode(true);
        }
        private void OnExecuteUninitialise()
        {
            if (IsStateReference())
            {
                //		m_pStation.GetPauseEventState()->SetCompleted();
                //		NxGlobal::GetLightTower()->ResetError();
                CallState(enumActivity.eReference);
            }

            if (m_bInitialise)
                BranchToState(enumActivity.eInitialise);

            m_pStation.SetCompletedSystemResetMode(false);
        }

        private void OnEnterInitialise()
        {
        }
        private void OnExecuteInitialise()
        {
        }

        private void OnEnterFault()
        {
        }
        private void OnExecuteFault()
        {
        }

        private void OnEnterEStop()
        {
        }

        private void OnExecuteEStop()
        {
        }

        private void OnEnterReference()
        {
        }

        private void OnExecuteReference()
        {
        }


        private void OnEnterPausing()
        {
        }
        private void OnExecutePausing()
        {

        }

        private void OnEnterPaused()
        {
        }
        private void OnExecutePaused()
        {
        }

        private void OnEnterStopping()
        {

        }
        private void OnExecuteStopping()
        {

        }

        private void OnEnterStopped()
        {
        }
        private void OnExecuteStopped()
        {

        }

        private void OnEnterRunning()
        {
        }
        private void OnExecuteRunning()
        {
        }

        private void OnEnterIsCassettePresent()
        {

        }
        private void OnExecuteIsCassettePresent()
        {
            if (GlobalVariable.bFrontRFID_Line2 == true)
            {

                if (!m_pStation.IsFrontRFIDAtLane2CassettePresentSensorActive())
                {
                    DoSetNextSequence();
                    ReturnFromState();
                }
            }
            else if (GlobalVariable.bRearRFID_Line2 == true)
            {

                if (m_pStation.IsRearRFIDAtLane2CassettePresentSensorActive())
                {
                    DoSetNextSequence();
                    ReturnFromState();
                }
            }

        }

        private void OnEnterReadRF()
        {
        }
        int CountDetail = 0;
        private void OnExecuteReadRF()
        {
            int nID = 0;

            if (GlobalVariable.bFrontRFID_Line2 == true)
            {
                nID = 1;
            }
            else if (GlobalVariable.bRearRFID_Line2 == true)
            {
                nID = 3;
            }

            if (GlobalVariable.CCE != null)
            {
                GlobalVariable.CCE.ReadRFDetailLane2(nID);
                CountDetail++;
            }

            Thread.Sleep(100);
            if (GlobalVariable.CCE != null)
                GlobalVariable.CCE.DisplayResultLane2();

            if (GlobalVariable.bDisplayLine2 || CountDetail>= 2)
            {
                DoSetNextSequence();
                ReturnFromState();
                CountDetail = 0;
            }
            else
            {
                if(GlobalVariable.CCE != null)
                GlobalVariable.CCE.ClearDisplay_2();
            }
            
        }

        

        private void OnEnterIsRemoveCassette()
        {
        }
        private void OnExecuteIsRemoveCassette()
        {
            if (GlobalVariable.bFrontRFID_Line2 == true)
            {
                if (m_pStation.IsFrontRFIDAtLane2CassettePresentSensorActive() )
                {
                    GlobalVariable.CCE.ClearDisplay_2();
                    DoSetNextSequence();
                    ReturnFromState();
                }           
            }
            else if (GlobalVariable.bRearRFID_Line2 == true)
            {
                if (!m_pStation.IsRearRFIDAtLane2CassettePresentSensorActive() )
                {
                    GlobalVariable.CCE.ClearDisplay_2();
                    DoSetNextSequence();
                    ReturnFromState();
                }           
            }
           
        }

        private void OnEnterCompleted()
        {
        }
        private void OnExecuteCompleted()
        {
            DoResetSequence();
            ReturnFromState();
        }

        private void OnEnterProduction()
        {
            m_b1stTimeSequenceEntry = false;
            m_bProduction = true;
        }

        private void OnExecuteProduction()
        {
            if ((eProductionMode[nSequence] == enumActivity.eCompleted))
                DoSequence(eProductionMode[nSequence]);
            else
                DoSequence(eProductionMode[nSequence]);
        }   

    }
}
