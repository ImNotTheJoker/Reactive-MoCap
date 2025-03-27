using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Reflection;

/// <summary>
/// Records human poses from an Animator component and exports them as AnimationClips.
/// Captures root movements, limb positions, rotations, and muscle activations at a specified sample rate.
/// </summary>
public class HumanPoseRecorder : MonoBehaviour
{
    [Header("Animator Settings")]
    /// <summary>
    /// The Animator component associated with the humanoid avatar.
    /// </summary>
    public Animator animator;

    [Header("Recording Control")]
    /// <summary>
    /// Indicates whether recording is currently active.
    /// </summary>
    private bool recording = false;

    /// <summary>
    /// The time when recording started.
    /// </summary>
    private float startTime;

    [Header("File Settings")]
    /// <summary>
    /// The path where the recorded AnimationClip will be saved.
    /// </summary>
    public string savePath = "Assets/Animations/";

    /// <summary>
    /// The name of the AnimationClip to be created.
    /// </summary>
    public string clipName = "MocapClip";

    [Header("Sampling Settings")]
    /// <summary>
    /// The sample rate in frames per second.
    /// </summary>
    public float sampleRate = 30.0f;

    /// <summary>
    /// The time interval between samples based on the sample rate.
    /// </summary>
    private float timeBetweenSamples;

    /// <summary>
    /// The timestamp of the last sample.
    /// </summary>
    private float lastSampleTime;

    [Header("Pose Handlers")]
    /// <summary>
    /// Handles the pose of the humanoid avatar.
    /// </summary>
    private HumanPoseHandler humanPoseHandler;

    /// <summary>
    /// Stores the current human pose data.
    /// </summary>
    private HumanPose humanPose;

    [Header("Root Position and Rotation Curves")]
    /// <summary>
    /// AnimationCurve for the root position along the X-axis.
    /// </summary>
    private AnimationCurve positionXCurve = new AnimationCurve();

    /// <summary>
    /// AnimationCurve for the root position along the Y-axis.
    /// </summary>
    private AnimationCurve positionYCurve = new AnimationCurve();

    /// <summary>
    /// AnimationCurve for the root position along the Z-axis.
    /// </summary>
    private AnimationCurve positionZCurve = new AnimationCurve();

    /// <summary>
    /// AnimationCurve for the root rotation around the X-axis.
    /// </summary>
    private AnimationCurve rotationXCurve = new AnimationCurve();

    /// <summary>
    /// AnimationCurve for the root rotation around the Y-axis.
    /// </summary>
    private AnimationCurve rotationYCurve = new AnimationCurve();

    /// <summary>
    /// AnimationCurve for the root rotation around the Z-axis.
    /// </summary>
    private AnimationCurve rotationZCurve = new AnimationCurve();

    /// <summary>
    /// AnimationCurve for the root rotation around the W-axis.
    /// </summary>
    private AnimationCurve rotationWCurve = new AnimationCurve();

    [Header("Left Foot Pose Curves")]
    private AnimationCurve leftFootTXCurve = new AnimationCurve();
    private AnimationCurve leftFootTYCurve = new AnimationCurve();
    private AnimationCurve leftFootTZCurve = new AnimationCurve();
    private AnimationCurve leftFootQXCurve = new AnimationCurve();
    private AnimationCurve leftFootQYCurve = new AnimationCurve();
    private AnimationCurve leftFootQZCurve = new AnimationCurve();
    private AnimationCurve leftFootQWCurve = new AnimationCurve();

    [Header("Right Foot Pose Curves")]
    private AnimationCurve rightFootTXCurve = new AnimationCurve();
    private AnimationCurve rightFootTYCurve = new AnimationCurve();
    private AnimationCurve rightFootTZCurve = new AnimationCurve();
    private AnimationCurve rightFootQXCurve = new AnimationCurve();
    private AnimationCurve rightFootQYCurve = new AnimationCurve();
    private AnimationCurve rightFootQZCurve = new AnimationCurve();
    private AnimationCurve rightFootQWCurve = new AnimationCurve();

    [Header("Left Hand Pose Curves")]
    private AnimationCurve leftHandTXCurve = new AnimationCurve();
    private AnimationCurve leftHandTYCurve = new AnimationCurve();
    private AnimationCurve leftHandTZCurve = new AnimationCurve();
    private AnimationCurve leftHandQXCurve = new AnimationCurve();
    private AnimationCurve leftHandQYCurve = new AnimationCurve();
    private AnimationCurve leftHandQZCurve = new AnimationCurve();
    private AnimationCurve leftHandQWCurve = new AnimationCurve();

    [Header("Right Hand Pose Curves")]
    private AnimationCurve rightHandTXCurve = new AnimationCurve();
    private AnimationCurve rightHandTYCurve = new AnimationCurve();
    private AnimationCurve rightHandTZCurve = new AnimationCurve();
    private AnimationCurve rightHandQXCurve = new AnimationCurve();
    private AnimationCurve rightHandQYCurve = new AnimationCurve();
    private AnimationCurve rightHandQZCurve = new AnimationCurve();
    private AnimationCurve rightHandQWCurve = new AnimationCurve();

    [Header("Muscle Activation Curves")]
    /// <summary>
    /// Array of AnimationCurves for muscle activations.
    /// </summary>
    private AnimationCurve[] muscleCurves;

    [Header("Muscle Mappings")]
    /// <summary>
    /// Dictionary mapping muscle names to their corresponding property paths.
    /// </summary>
    public static Dictionary<string, string> TraitPropMap = new Dictionary<string, string>
    {
        {"Left Thumb 1 Stretched", "LeftHand.Thumb.1 Stretched"},
        {"Left Thumb Spread", "LeftHand.Thumb.Spread"},
        {"Left Thumb 2 Stretched", "LeftHand.Thumb.2 Stretched"},
        {"Left Thumb 3 Stretched", "LeftHand.Thumb.3 Stretched"},
        {"Left Index 1 Stretched", "LeftHand.Index.1 Stretched"},
        {"Left Index Spread", "LeftHand.Index.Spread"},
        {"Left Index 2 Stretched", "LeftHand.Index.2 Stretched"},
        {"Left Index 3 Stretched", "LeftHand.Index.3 Stretched"},
        {"Left Middle 1 Stretched", "LeftHand.Middle.1 Stretched"},
        {"Left Middle Spread", "LeftHand.Middle.Spread"},
        {"Left Middle 2 Stretched", "LeftHand.Middle.2 Stretched"},
        {"Left Middle 3 Stretched", "LeftHand.Middle.3 Stretched"},
        {"Left Ring 1 Stretched", "LeftHand.Ring.1 Stretched"},
        {"Left Ring Spread", "LeftHand.Ring.Spread"},
        {"Left Ring 2 Stretched", "LeftHand.Ring.2 Stretched"},
        {"Left Ring 3 Stretched", "LeftHand.Ring.3 Stretched"},
        {"Left Little 1 Stretched", "LeftHand.Little.1 Stretched"},
        {"Left Little Spread", "LeftHand.Little.Spread"},
        {"Left Little 2 Stretched", "LeftHand.Little.2 Stretched"},
        {"Left Little 3 Stretched", "LeftHand.Little.3 Stretched"},
        {"Right Thumb 1 Stretched", "RightHand.Thumb.1 Stretched"},
        {"Right Thumb Spread", "RightHand.Thumb.Spread"},
        {"Right Thumb 2 Stretched", "RightHand.Thumb.2 Stretched"},
        {"Right Thumb 3 Stretched", "RightHand.Thumb.3 Stretched"},
        {"Right Index 1 Stretched", "RightHand.Index.1 Stretched"},
        {"Right Index Spread", "RightHand.Index.Spread"},
        {"Right Index 2 Stretched", "RightHand.Index.2 Stretched"},
        {"Right Index 3 Stretched", "RightHand.Index.3 Stretched"},
        {"Right Middle 1 Stretched", "RightHand.Middle.1 Stretched"},
        {"Right Middle Spread", "RightHand.Middle.Spread"},
        {"Right Middle 2 Stretched", "RightHand.Middle.2 Stretched"},
        {"Right Middle 3 Stretched", "RightHand.Middle.3 Stretched"},
        {"Right Ring 1 Stretched", "RightHand.Ring.1 Stretched"},
        {"Right Ring Spread", "RightHand.Ring.Spread"},
        {"Right Ring 2 Stretched", "RightHand.Ring.2 Stretched"},
        {"Right Ring 3 Stretched", "RightHand.Ring.3 Stretched"},
        {"Right Little 1 Stretched", "RightHand.Little.1 Stretched"},
        {"Right Little Spread", "RightHand.Little.Spread"},
        {"Right Little 2 Stretched", "RightHand.Little.2 Stretched"},
        {"Right Little 3 Stretched", "RightHand.Little.3 Stretched"},
    };

    /// <summary>
    /// Initializes the HumanPoseHandler and muscle curves.
    /// </summary>
    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            Debug.LogError("Animator not assigned and not found on GameObject.");
            return;
        }

        humanPoseHandler = new HumanPoseHandler(animator.avatar, animator.transform);
        humanPose = new HumanPose();

        muscleCurves = new AnimationCurve[HumanTrait.MuscleCount];
        for (int i = 0; i < HumanTrait.MuscleCount; i++)
        {
            muscleCurves[i] = new AnimationCurve();
        }

        timeBetweenSamples = 1.0f / sampleRate;
    }

    /// <summary>
    /// Records poses at fixed intervals while recording is active.
    /// </summary>
    void FixedUpdate()
    {
        if (recording)
        {
            if (Time.time - lastSampleTime >= timeBetweenSamples)
            {
                humanPoseHandler.GetHumanPose(ref humanPose);
                float time = Time.time - startTime;
                RecordCurrentPose(time);
                lastSampleTime = Time.time;
            }
        }
    }

    /// <summary>
    /// Starts recording poses and initializes recording parameters.
    /// </summary>
    /// <param name="folderPath">The folder path where the AnimationClip will be saved.</param>
    /// <param name="clipName">The name of the AnimationClip.</param>
    public void StartRecording(string folderPath, string clipName)
    {
        if (recording)
        {
            Debug.LogWarning("Already recording.");
            return;
        }

        this.savePath = folderPath;
        this.clipName = clipName;

        // Clear all existing curves
        ClearCurves();
        recording = true;
        startTime = Time.time;
        lastSampleTime = 0f;
        Debug.Log("Recording started.");
    }

    /// <summary>
    /// Stops recording poses and exports the recorded data as an AnimationClip.
    /// </summary>
    public void StopRecording()
    {
        if (!recording)
        {
            Debug.LogWarning("Not currently recording.");
            return;
        }

        recording = false;
        Debug.Log("Recording stopped, exporting animation...");
        ExportHumanoidAnim();
    }

    /// <summary>
    /// Clears all AnimationCurves to prepare for a new recording session.
    /// </summary>
    private void ClearCurves()
    {
        positionXCurve = new AnimationCurve();
        positionYCurve = new AnimationCurve();
        positionZCurve = new AnimationCurve();
        rotationXCurve = new AnimationCurve();
        rotationYCurve = new AnimationCurve();
        rotationZCurve = new AnimationCurve();
        rotationWCurve = new AnimationCurve();

        leftFootTXCurve = new AnimationCurve();
        leftFootTYCurve = new AnimationCurve();
        leftFootTZCurve = new AnimationCurve();
        leftFootQXCurve = new AnimationCurve();
        leftFootQYCurve = new AnimationCurve();
        leftFootQZCurve = new AnimationCurve();
        leftFootQWCurve = new AnimationCurve();

        rightFootTXCurve = new AnimationCurve();
        rightFootTYCurve = new AnimationCurve();
        rightFootTZCurve = new AnimationCurve();
        rightFootQXCurve = new AnimationCurve();
        rightFootQYCurve = new AnimationCurve();
        rightFootQZCurve = new AnimationCurve();
        rightFootQWCurve = new AnimationCurve();

        leftHandTXCurve = new AnimationCurve();
        leftHandTYCurve = new AnimationCurve();
        leftHandTZCurve = new AnimationCurve();
        leftHandQXCurve = new AnimationCurve();
        leftHandQYCurve = new AnimationCurve();
        leftHandQZCurve = new AnimationCurve();
        leftHandQWCurve = new AnimationCurve();

        rightHandTXCurve = new AnimationCurve();
        rightHandTYCurve = new AnimationCurve();
        rightHandTZCurve = new AnimationCurve();
        rightHandQXCurve = new AnimationCurve();
        rightHandQYCurve = new AnimationCurve();
        rightHandQZCurve = new AnimationCurve();
        rightHandQWCurve = new AnimationCurve();

        muscleCurves = new AnimationCurve[HumanTrait.MuscleCount];
        for (int i = 0; i < HumanTrait.MuscleCount; i++)
        {
            muscleCurves[i] = new AnimationCurve();
        }
    }

    /// <summary>
    /// Records the current pose data into the respective AnimationCurves.
    /// </summary>
    /// <param name="time">The timestamp for the current sample.</param>
    private void RecordCurrentPose(float time)
    {
        // Record root position and rotation
        positionXCurve.AddKey(time, humanPose.bodyPosition.x);
        positionYCurve.AddKey(time, humanPose.bodyPosition.y);
        positionZCurve.AddKey(time, humanPose.bodyPosition.z);

        rotationXCurve.AddKey(time, humanPose.bodyRotation.x);
        rotationYCurve.AddKey(time, humanPose.bodyRotation.y);
        rotationZCurve.AddKey(time, humanPose.bodyRotation.z);
        rotationWCurve.AddKey(time, humanPose.bodyRotation.w);

        // Record foot and hand poses (normalized relative to Humanoid Root)
        RecordFootAndHandPose(time, "LeftFoot", ref leftFootTXCurve, ref leftFootTYCurve, ref leftFootTZCurve, ref leftFootQXCurve, ref leftFootQYCurve, ref leftFootQZCurve, ref leftFootQWCurve);
        RecordFootAndHandPose(time, "RightFoot", ref rightFootTXCurve, ref rightFootTYCurve, ref rightFootTZCurve, ref rightFootQXCurve, ref rightFootQYCurve, ref rightFootQZCurve, ref rightFootQWCurve);
        RecordFootAndHandPose(time, "LeftHand", ref leftHandTXCurve, ref leftHandTYCurve, ref leftHandTZCurve, ref leftHandQXCurve, ref leftHandQYCurve, ref leftHandQZCurve, ref leftHandQWCurve);
        RecordFootAndHandPose(time, "RightHand", ref rightHandTXCurve, ref rightHandTYCurve, ref rightHandTZCurve, ref rightHandQXCurve, ref rightHandQYCurve, ref rightHandQZCurve, ref rightHandQWCurve);

        // Record muscle activations
        for (int i = 0; i < HumanTrait.MuscleCount; i++)
        {
            muscleCurves[i].AddKey(time, humanPose.muscles[i]);
        }
    }

    /// <summary>
    /// Records the pose of a specific foot or hand into the respective AnimationCurves.
    /// </summary>
    /// <param name="time">The timestamp for the current sample.</param>
    /// <param name="targetName">The name of the target (e.g., "LeftFoot").</param>
    /// <param name="txCurve">AnimationCurve for translation X.</param>
    /// <param name="tyCurve">AnimationCurve for translation Y.</param>
    /// <param name="tzCurve">AnimationCurve for translation Z.</param>
    /// <param name="qxCurve">AnimationCurve for rotation X.</param>
    /// <param name="qyCurve">AnimationCurve for rotation Y.</param>
    /// <param name="qzCurve">AnimationCurve for rotation Z.</param>
    /// <param name="qwCurve">AnimationCurve for rotation W.</param>
    private void RecordFootAndHandPose(float time, string targetName, ref AnimationCurve txCurve, ref AnimationCurve tyCurve, ref AnimationCurve tzCurve, ref AnimationCurve qxCurve, ref AnimationCurve qyCurve, ref AnimationCurve qzCurve, ref AnimationCurve qwCurve)
    {
        AvatarIKGoal ikGoal;
        switch (targetName)
        {
            case "LeftFoot":
                ikGoal = AvatarIKGoal.LeftFoot;
                break;
            case "RightFoot":
                ikGoal = AvatarIKGoal.RightFoot;
                break;
            case "LeftHand":
                ikGoal = AvatarIKGoal.LeftHand;
                break;
            case "RightHand":
                ikGoal = AvatarIKGoal.RightHand;
                break;
            default:
                Debug.LogError($"Unknown targetName: {targetName}");
                return;
        }

        // Retrieve IK target position and rotation
        TQ bodyTQ = new TQ(humanPose.bodyPosition, humanPose.bodyRotation);
        TQ skeletonTQ;

        if (ikGoal == AvatarIKGoal.LeftFoot || ikGoal == AvatarIKGoal.RightFoot)
        {
            HumanBodyBones bone = ikGoal == AvatarIKGoal.LeftFoot ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot;
            Transform boneTransform = animator.GetBoneTransform(bone);
            if (boneTransform == null)
            {
                Debug.LogError($"{bone} transform not found.");
                return;
            }
            skeletonTQ = new TQ(boneTransform.position, boneTransform.rotation);
        }
        else
        {
            HumanBodyBones bone = ikGoal == AvatarIKGoal.LeftHand ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
            Transform boneTransform = animator.GetBoneTransform(bone);
            if (boneTransform == null)
            {
                Debug.LogError($"{bone} transform not found.");
                return;
            }
            skeletonTQ = new TQ(boneTransform.position, boneTransform.rotation);
        }

        TQ goalTQ = AvatarUtility.GetIKGoalTQ(animator.avatar, animator.humanScale, ikGoal, bodyTQ, skeletonTQ);

        // Calculate relative position and rotation
        Vector3 relativePosition = goalTQ.t;
        Quaternion relativeRotation = goalTQ.q;

        // Add keys to the curves
        txCurve.AddKey(time, relativePosition.x);
        tyCurve.AddKey(time, relativePosition.y);
        tzCurve.AddKey(time, relativePosition.z);

        qxCurve.AddKey(time, relativeRotation.x);
        qyCurve.AddKey(time, relativeRotation.y);
        qzCurve.AddKey(time, relativeRotation.z);
        qwCurve.AddKey(time, relativeRotation.w);
    }

    /// <summary>
    /// Exports the recorded poses as an AnimationClip and saves it to the specified path.
    /// </summary>
    public void ExportHumanoidAnim()
    {
        if (positionXCurve.length == 0)
        {
            Debug.LogWarning("No poses recorded.");
            return;
        }

        AnimationClip clip = new AnimationClip
        {
            name = clipName,
            legacy = false // Ensure compatibility with Mecanim
        };

        // Assign root position and rotation curves
        clip.SetCurve("", typeof(Animator), "RootT.x", positionXCurve);
        clip.SetCurve("", typeof(Animator), "RootT.y", positionYCurve);
        clip.SetCurve("", typeof(Animator), "RootT.z", positionZCurve);
        clip.SetCurve("", typeof(Animator), "RootQ.x", rotationXCurve);
        clip.SetCurve("", typeof(Animator), "RootQ.y", rotationYCurve);
        clip.SetCurve("", typeof(Animator), "RootQ.z", rotationZCurve);
        clip.SetCurve("", typeof(Animator), "RootQ.w", rotationWCurve);

        // Assign foot and hand pose curves
        SetFootAndHandCurves(clip, "LeftFoot", leftFootTXCurve, leftFootTYCurve, leftFootTZCurve, leftFootQXCurve, leftFootQYCurve, leftFootQZCurve, leftFootQWCurve);
        SetFootAndHandCurves(clip, "RightFoot", rightFootTXCurve, rightFootTYCurve, rightFootTZCurve, rightFootQXCurve, rightFootQYCurve, rightFootQZCurve, rightFootQWCurve);
        SetFootAndHandCurves(clip, "LeftHand", leftHandTXCurve, leftHandTYCurve, leftHandTZCurve, leftHandQXCurve, leftHandQYCurve, leftHandQZCurve, leftHandQWCurve);
        SetFootAndHandCurves(clip, "RightHand", rightHandTXCurve, rightHandTYCurve, rightHandTZCurve, rightHandQXCurve, rightHandQYCurve, rightHandQZCurve, rightHandQWCurve);

        // Assign muscle activation curves
        for (int i = 0; i < HumanTrait.MuscleCount; i++)
        {
            string muscleName = HumanTrait.MuscleName[i];
            if (TraitPropMap.TryGetValue(muscleName, out string mappedName))
            {
                clip.SetCurve("", typeof(Animator), mappedName, muscleCurves[i]);
            }
            else
            {
                clip.SetCurve("", typeof(Animator), muscleName, muscleCurves[i]);
            }
        }

        clip.EnsureQuaternionContinuity();

#if UNITY_EDITOR
        // Save the AnimationClip asset
        string finalPath = $"{savePath}/{clipName}.anim";
        AssetDatabase.CreateAsset(clip, finalPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Load the saved AnimationClip to modify settings
        AnimationClip savedClip = (AnimationClip)AssetDatabase.LoadAssetAtPath(finalPath, typeof(AnimationClip));
        if (savedClip != null)
        {
            SerializedObject serializedClip = new SerializedObject(savedClip);

            // Access AnimationClip settings
            SerializedProperty clipSettings = serializedClip.FindProperty("m_AnimationClipSettings");
            if (clipSettings != null)
            {
                // Set LoopBlendOrientation
                SerializedProperty loopBlendOrientation = clipSettings.FindPropertyRelative("m_LoopBlendOrientation");
                if (loopBlendOrientation != null)
                {
                    loopBlendOrientation.boolValue = true;
                }
                else
                {
                    Debug.LogWarning("Property 'm_LoopBlendOrientation' not found.");
                }

                // Set LoopBlendPositionY
                SerializedProperty loopBlendPositionY = clipSettings.FindPropertyRelative("m_LoopBlendPositionY");
                if (loopBlendPositionY != null)
                {
                    loopBlendPositionY.boolValue = true;
                }
                else
                {
                    Debug.LogWarning("Property 'm_LoopBlendPositionY' not found.");
                }

                // Apply changes
                serializedClip.ApplyModifiedProperties();
                EditorUtility.SetDirty(savedClip);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"Animation Clip exported successfully to {finalPath} with LoopBlend settings.");
            }
            else
            {
                Debug.LogWarning("Property 'm_AnimationClipSettings' not found.");
            }
        }
        else
        {
            Debug.LogError($"AnimationClip could not be loaded from path {finalPath}.");
        }
#endif

        Debug.Log($"Animation Clip exported successfully to {savePath}/{clipName}.anim");
    }

    /// <summary>
    /// Assigns the foot or hand pose curves to the AnimationClip.
    /// </summary>
    /// <param name="clip">The AnimationClip to assign curves to.</param>
    /// <param name="targetName">The name of the target (e.g., "LeftFoot").</param>
    /// <param name="txCurve">AnimationCurve for translation X.</param>
    /// <param name="tyCurve">AnimationCurve for translation Y.</param>
    /// <param name="tzCurve">AnimationCurve for translation Z.</param>
    /// <param name="qxCurve">AnimationCurve for rotation X.</param>
    /// <param name="qyCurve">AnimationCurve for rotation Y.</param>
    /// <param name="qzCurve">AnimationCurve for rotation Z.</param>
    /// <param name="qwCurve">AnimationCurve for rotation W.</param>
    private void SetFootAndHandCurves(AnimationClip clip, string targetName, AnimationCurve txCurve, AnimationCurve tyCurve, AnimationCurve tzCurve, AnimationCurve qxCurve, AnimationCurve qyCurve, AnimationCurve qzCurve, AnimationCurve qwCurve)
    {
        clip.SetCurve("", typeof(Animator), $"{targetName}T.x", txCurve);
        clip.SetCurve("", typeof(Animator), $"{targetName}T.y", tyCurve);
        clip.SetCurve("", typeof(Animator), $"{targetName}T.z", tzCurve);
        clip.SetCurve("", typeof(Animator), $"{targetName}Q.x", qxCurve);
        clip.SetCurve("", typeof(Animator), $"{targetName}Q.y", qyCurve);
        clip.SetCurve("", typeof(Animator), $"{targetName}Q.z", qzCurve);
        clip.SetCurve("", typeof(Animator), $"{targetName}Q.w", qwCurve);
    }

    /// <summary>
    /// Represents a translation and rotation pair.
    /// </summary>
    public class TQ
    {
        /// <summary>
        /// Translation vector.
        /// </summary>
        public Vector3 t;

        /// <summary>
        /// Rotation quaternion.
        /// </summary>
        public Quaternion q;

        /// <summary>
        /// Initializes a new instance of the TQ class with specified translation and rotation.
        /// </summary>
        /// <param name="translation">The translation vector.</param>
        /// <param name="rotation">The rotation quaternion.</param>
        public TQ(Vector3 translation, Quaternion rotation)
        {
            t = translation;
            q = rotation;
        }
    }

    /// <summary>
    /// Utility class for avatar-related operations.
    /// </summary>
    public class AvatarUtility
    {
        /// <summary>
        /// Calculates the IK goal's translation and rotation based on the avatar's scale and current poses.
        /// </summary>
        /// <param name="avatar">The avatar to calculate for.</param>
        /// <param name="humanScale">The scale of the humanoid.</param>
        /// <param name="avatarIKGoal">The IK goal (e.g., LeftFoot).</param>
        /// <param name="animatorBodyPositionRotation">The animator's body position and rotation.</param>
        /// <param name="skeletonTQ">The skeleton's translation and rotation.</param>
        /// <returns>A TQ object representing the IK goal's translation and rotation.</returns>
        public static TQ GetIKGoalTQ(Avatar avatar, float humanScale, AvatarIKGoal avatarIKGoal, TQ animatorBodyPositionRotation, TQ skeletonTQ)
        {
            int humanId = (int)HumanIDFromAvatarIKGoal(avatarIKGoal);
            if (humanId == (int)HumanBodyBones.LastBone)
                throw new InvalidOperationException("Invalid human id.");

            MethodInfo methodGetAxisLength = typeof(Avatar).GetMethod("GetAxisLength", BindingFlags.Instance | BindingFlags.NonPublic);
            if (methodGetAxisLength == null)
                throw new InvalidOperationException("Cannot find GetAxisLength method.");

            MethodInfo methodGetPostRotation = typeof(Avatar).GetMethod("GetPostRotation", BindingFlags.Instance | BindingFlags.NonPublic);
            if (methodGetPostRotation == null)
                throw new InvalidOperationException("Cannot find GetPostRotation method.");

            Quaternion postRotation = (Quaternion)methodGetPostRotation.Invoke(avatar, new object[] { humanId });
            TQ goalTQ = new TQ(skeletonTQ.t, skeletonTQ.q * postRotation);

            if (avatarIKGoal == AvatarIKGoal.LeftFoot || avatarIKGoal == AvatarIKGoal.RightFoot)
            {
                float axisLength = (float)methodGetAxisLength.Invoke(avatar, new object[] { humanId });
                Vector3 footBottom = new Vector3(axisLength, 0, 0);
                goalTQ.t += (goalTQ.q * footBottom);
            }

            // IK goals are in avatar body local space
            Quaternion invRootQ = Quaternion.Inverse(animatorBodyPositionRotation.q);
            goalTQ.t = invRootQ * (goalTQ.t - animatorBodyPositionRotation.t);
            goalTQ.q = invRootQ * goalTQ.q;
            goalTQ.t /= humanScale;

            return goalTQ;
        }

        /// <summary>
        /// Maps an AvatarIKGoal to the corresponding HumanBodyBones enum.
        /// </summary>
        /// <param name="avatarIKGoal">The AvatarIKGoal to map.</param>
        /// <returns>The corresponding HumanBodyBones value.</returns>
        public static HumanBodyBones HumanIDFromAvatarIKGoal(AvatarIKGoal avatarIKGoal)
        {
            HumanBodyBones humanId = HumanBodyBones.LastBone;
            switch (avatarIKGoal)
            {
                case AvatarIKGoal.LeftFoot:
                    humanId = HumanBodyBones.LeftFoot;
                    break;
                case AvatarIKGoal.RightFoot:
                    humanId = HumanBodyBones.RightFoot;
                    break;
                case AvatarIKGoal.LeftHand:
                    humanId = HumanBodyBones.LeftHand;
                    break;
                case AvatarIKGoal.RightHand:
                    humanId = HumanBodyBones.RightHand;
                    break;
            }
            return humanId;
        }
    }
}
