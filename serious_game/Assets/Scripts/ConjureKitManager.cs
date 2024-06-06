using Auki.ConjureKit;
using Auki.ConjureKit.Manna;
using Auki.Integration.ARFoundation.Manna;
using Auki.Util;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ConjureKitManager : MonoBehaviour
{
    public static ConjureKitManager instance;

    [SerializeField] private Camera arCamera;
    [SerializeField] private Button startButton;

    public uint battlefieldEntityID = 0;
    public event System.Action onPhonePosSync = delegate { };
    public IConjureKit _conjureKit;
    private Manna _manna;
    private FrameFeederGPU _arCameraFrameFeeder;
    private ARCameraManager arCameraManager;
    private Texture2D _videoTexture;
    private Pose identityPose = Pose.identity;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            //DontDestroyOnLoad(gameObject);
            gameObject.SetActive(false);
        }
    }

    void Start()
    {
        arCameraManager = arCamera.GetComponent<ARCameraManager>();

        _conjureKit = new ConjureKit(
            arCamera.transform,
            "a26617cb-abdc-4ff4-a18f-856478813a48",
            "307880bd-8c40-4035-922e-92c0a478d327ef96bcc3-1602-4ce9-97ff-34485d49bf79");

        _manna = new Manna(_conjureKit);

        _arCameraFrameFeeder = _manna.GetOrCreateFrameFeederComponent();
        _arCameraFrameFeeder.AttachMannaInstance(_manna);
        _manna.SetLighthousePosition(new Vector2(Screen.width / 2f, 2 * Screen.height / 3f));
        if (NetworkManager.instance.photonView.IsMine)
        {
            _conjureKit.OnParticipantJoined += participant =>
            {
                if (participant.Id == _conjureKit.GetSession().ParticipantId)
                {
                    return;
                }
                ToggleLighthouse(false);
                CreateBattlefieldEntity();
            };
        }
        _conjureKit.OnEntityAdded += entity =>
        {
            Debug.Log("Entity Added with participant ID - " + entity.ParticipantId + " and flag = " + entity.Flag.ToString());
            ToggleLighthouse(false);
            CreateBattlefield(entity, 1f);
        };

        _conjureKit.OnStateChanged += state =>
        {
            if (_conjureKit.GetSession() != null && _conjureKit.GetSession().GetParticipantCount() > 1)
            {
                return;
            }
            ToggleLighthouse(state == State.Calibrated);
        };


        _conjureKit.Connect();
    }
    private void Update()
    {
        //FeedMannaWithVideoFrames();
    }

    private void FeedMannaWithVideoFrames()
    {
        var imageAcquired = arCameraManager.TryAcquireLatestCpuImage(out var cpuImage);
        if (!imageAcquired)
        {
            AukiDebug.LogInfo("Couldn't acquire CPU image");
            return;
        }

        if (_videoTexture == null) _videoTexture = new Texture2D(cpuImage.width, cpuImage.height, TextureFormat.R8, false);

        var conversionParams = new XRCpuImage.ConversionParams(cpuImage, TextureFormat.R8);
        cpuImage.ConvertAsync(
            conversionParams,
            (status, @params, buffer) =>
            {
                _videoTexture.SetPixelData(buffer, 0, 0);
                _videoTexture.Apply();
                cpuImage.Dispose();

                _manna.ProcessVideoFrameTexture(
                    _videoTexture,
                    arCamera.projectionMatrix,
                    arCamera.worldToCameraMatrix
                );
            }
        );
    }


    public void ToggleLighthouse(bool enable)
    {
        startButton.interactable = true;
        _manna.SetLighthouseVisible(enable && NetworkManager.instance.photonView.IsMine);
    }

    public void CreateBattlefieldEntity(Pose entityPos = default(Pose), float scale = 1f)
    {
        if (_conjureKit.GetState() != State.Calibrated || _conjureKit.GetSession().GetEntityCount() > 3)
            return;

        if (entityPos == default(Pose))
        {
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            entityPos = new Pose(position, rotation);
        }
        _conjureKit.GetSession().AddEntity(
            entityPos,
            onComplete: entity => CreateBattlefield(entity, 1f),
            onError: error => Debug.Log(error));

    }

    public void SetEntityPoseAndScale(Pose pose, float scale)
    {
        var session = _conjureKit.GetSession();
        Entity entity = session.GetEntity(battlefieldEntityID);
        _conjureKit.GetSession().SetEntityPose(entity.Id, pose);
        session.UpdateComponent(entity.Id, 0, BitConverter.GetBytes(scale));
    }

    private void CreateBattlefield(Entity entity, float scale)
    {
        if (entity.Flag == EntityFlag.EntityFlagParticipantEntity)
        {
            return;
        }
        var session = _conjureKit.GetSession();
        var pose = session.GetEntityPose(entity);
        BattlefieldManager.instance.SpawnBattlefield(pose.position, pose.rotation, 1f);
        BattlefieldManager.instance.ChangeBattlefieldSize(scale);
        Debug.Log("Battlefield Added with participant ID - " + entity.ParticipantId + " and flag = " + entity.Flag.ToString());
        EntityComponent entityComponent = new EntityComponent(0, entity.Id, BitConverter.GetBytes(1f));
        if (!entity.Components.ContainsKey(0))
        {
            entity.Components.Add(0, entityComponent);
        }
        session.GetEntities().ForEach(e =>
        {
            if (e.Flag == EntityFlag.EntityFlagEmpty)
            {
                battlefieldEntityID = e.Id;
            }
        });
        onPhonePosSync();
    }

    public void UpdateEntity(uint entityID)
    {
        entityID = battlefieldEntityID;
        var session = _conjureKit.GetSession();
        var pose = session.GetEntityPose(entityID);
        float scale = BitConverter.ToSingle(session.GetEntityComponent(entityID, 0).Data);
        BattlefieldManager.instance.battlefieldGameObject.transform.position = pose.position;
        BattlefieldManager.instance.battlefieldGameObject.transform.rotation = pose.rotation;
        //BattlefieldManager.instance.ChangeBattlefieldSize(scale);
    }
}