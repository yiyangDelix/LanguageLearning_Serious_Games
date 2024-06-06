using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlacementManager : MonoBehaviour
{

    ARRaycastManager m_ARRaycastManager;
    ARAnchorManager m_ARAnchorManager;
    static List<ARRaycastHit> raycast_Hits = new List<ARRaycastHit>();

    public Camera aRCamera;
    public Button placeButton;



    private GameObject battlefieldGameObject;
    private void Awake()
    {
        m_ARRaycastManager = GetComponent<ARRaycastManager>();
        m_ARAnchorManager = GetComponent<ARAnchorManager>();

    }
    private void Start()
    {
        BattlefieldManager.instance.onBattlefieldSpawned += SetBattlefield;
    }


    void SetBattlefield(GameObject battlefield)
    {
        battlefieldGameObject = battlefield;
    }

    // Update is called once per frame
    void Update()
    {
        if (battlefieldGameObject == null)
        {
            return;
        }
        Vector3 centerOfScreen = new Vector3(Screen.width / 2, Screen.height / 2);
        Ray ray = aRCamera.ScreenPointToRay(centerOfScreen);

        if (m_ARRaycastManager.Raycast(ray, raycast_Hits, TrackableType.PlaneWithinPolygon))
        {
            if(battlefieldGameObject.activeSelf == false)
            {
                battlefieldGameObject.SetActive(true);
            }
            //Intersection!
            UnityEngine.Pose hitPose = raycast_Hits[0].pose;

            Vector3 positionToBePlaced = hitPose.position;
            if(placeButton.interactable == false)
            {
                placeButton.interactable = true;
            }
            battlefieldGameObject.transform.position = positionToBePlaced;
            hitPose.rotation = battlefieldGameObject.transform.rotation;
            //ConjureKitManager.instance.SetEntityPoseAndScale(hitPose, battlefieldGameObject.transform.localScale.x);
            //battlefieldGameObject.transform.LookAt(Vector3.zero);
            //battlefieldGameObject.transform.localEulerAngles = new Vector3(0, battlefieldGameObject.transform.localEulerAngles.y, 0);
        }
        else
        {
#if !UNITY_EDITOR
            if (battlefieldGameObject.activeSelf == true)
            {
                battlefieldGameObject.SetActive(false);
            }
            if (placeButton.interactable == true)
            {
                placeButton.interactable = false;
            }
#endif
        }
    }
}
