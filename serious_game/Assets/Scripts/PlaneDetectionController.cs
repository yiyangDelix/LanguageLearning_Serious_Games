using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class PlaneDetectionController : MonoBehaviour
{
    ARPlaneManager m_ARPlaneManager;
    ARPlacementManager m_ARPlacementManager;

    public GameObject placeButton;

    public TextMeshProUGUI informUIPanel_Text;


    public static event System.Action onPlacedBattlefield = delegate { };

    private void Awake()
    {
        m_ARPlaneManager = GetComponent<ARPlaneManager>();
        m_ARPlacementManager = GetComponent<ARPlacementManager>();

    }


    // Start is called before the first frame update
    void Start()
    {
        if (placeButton != null)
        {
            placeButton.SetActive(true);
#if !UNITY_EDITOR
            placeButton.GetComponent<Button>().interactable = false;
#endif
        }



        NetworkManager.instance.onReadyToStartBattlefieldPlacement += EnableARPlacementAndPlaneDetection;

        m_ARPlaneManager.enabled = true;
        onPlacedBattlefield += () => AudioManager.instance.Play(SoundType.CardPlaced);
        ConjureKitManager.instance.onPhonePosSync += EnableDetection;
    }

    private void EnableDetection()
    {
        if (PhotonNetwork.IsConnected)
        {
            if (!NetworkManager.instance.photonView.IsMine)
            {
                DisableARPlacementAndPlaneDetection();
                BattlefieldManager.instance.SyncBattlefield(true);
            }
            else
            {
                EnableARPlacementAndPlaneDetection();
            }
            ConjureKitManager.instance.onPhonePosSync -= EnableDetection;
        }
    }


    public void DisableARPlacementAndPlaneDetection()
    {
        m_ARPlaneManager.planePrefab = null;
        m_ARPlaneManager.enabled = false;
        m_ARPlacementManager.enabled = false;
        SetAllPlanesActiveOrDeactive(false);
        BattlefieldManager.instance.TurnOffInteraction();
        placeButton.SetActive(false);
        if (NetworkManager.instance.useConjureKit)
        {
            if (PhotonNetwork.LocalPlayer.IsMasterClient)
            {
                var battlefieldTransform = BattlefieldManager.instance.battlefieldGameObject.transform;
                var pose = new Pose(battlefieldTransform.position, battlefieldTransform.rotation);
                //ConjureKitManager.instance.SetEntityPoseAndScale(pose, battlefieldTransform.localScale.x);
                informUIPanel_Text.text = "Great! You placed the Battlefield.";

                onPlacedBattlefield();
            }
            else
            {
                if (BattlefieldManager.instance.battlefieldGameObject == null)
                {
                    informUIPanel_Text.text = "Waiting for host to place the Battlefield...";
                }
                else
                {
                    informUIPanel_Text.text = "Great! You have a Battlefield now that you can scale. Wait for the host to place it.";
                }

            }
        }
        else
        {
            //BattlefieldManager.instance.battlefieldGameObject.AddComponent<ARAnchor>();
            onPlacedBattlefield();
        }

    }

    public void EnableARPlacementAndPlaneDetection()
    {
        //BattlefieldManager.instance.SpawnBattlefield(Vector3.zero, Quaternion.identity, 1f);

        if (BattlefieldManager.instance.battlefieldGameObject == null || !PhotonNetwork.IsConnected)
        {
            return;
        }

#if UNITY_EDITOR
        BattlefieldManager.instance.battlefieldGameObject.SetActive(true);// Turn this off for build
#endif
        AudioManager.instance.Play(SoundType.CardPlaced);
        m_ARPlacementManager.enabled = true;
        SetAllPlanesActiveOrDeactive(true);

        placeButton.SetActive(true);

        placeButton.GetComponent<Button>().onClick.AddListener(() =>
        AudioManager.instance.PlayButtonClickSounds());
        //informUIPanel_Text.transform.parent.gameObject.SetActive(false);
        informUIPanel_Text.text = "Move phone to detect planes and place the Battlefield. Use twist and pinch gestures to rotate and scale the Battlefield.";
    }




    private void SetAllPlanesActiveOrDeactive(bool value)
    {
        foreach (var plane in m_ARPlaneManager.trackables)
        {
            plane.gameObject.SetActive(value);
        }
    }
}
