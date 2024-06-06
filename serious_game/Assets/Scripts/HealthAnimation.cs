using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class HealthAnimation : MonoBehaviour
{
    [SerializeField] private GameObject healthObjectPrefab;
    [SerializeField] private List<Transform> playerHealthObjects = new List<Transform>();
    [SerializeField] private List<Transform> enemyHealthObjects = new List<Transform>();
    [SerializeField] private TMPro.TextMeshPro[] healthTexts = new TMPro.TextMeshPro[2];
    [SerializeField] private TMPro.TextMeshPro[] damageTexts = new TMPro.TextMeshPro[2];
    [SerializeField] private GameObject healthTextPrefab;

    private int maxColumns = 5;
    private Vector2 distanceBetweenObjects = new Vector2(-1.0f, 1.0f);
    private Vector3 playerHealthObjectStartPos = new Vector3(-8f, 0, -8f); // Bottom left corner of board
    private Vector3 enemyHealthObjectStartPos = new Vector3(-8f, 0, 8f);// Top left corner of board
    private float healthObjectScale = 0.5f;
    public void GenerateHealthObjects(CardOwner owner, int maxHealth, Vector3 startPos, Transform parent)
    {
        List<Transform> healthObjects;
        int yDirection;
        if (owner == CardOwner.Player)
        {
            startPos -= new Vector3(0, 0, distanceBetweenObjects.y * ((maxHealth / (2f * maxColumns)) - 1));
            playerHealthObjectStartPos = startPos;
            healthObjects = playerHealthObjects;
            yDirection = 1;
        }
        else
        {
            startPos += new Vector3(0, 0, distanceBetweenObjects.y * ((maxHealth / (2f * maxColumns)) - 1));
            enemyHealthObjectStartPos = startPos;
            healthObjects = enemyHealthObjects;
            yDirection = -1;
        }
        for (int i = 0; i < maxHealth / maxColumns; i++)
        {
            for (int j = 0; j < maxColumns; j++)
            {
                GameObject healthObject = Instantiate(healthObjectPrefab, Vector3.zero, Quaternion.identity, parent);
                healthObject.transform.localPosition = startPos + new Vector3(distanceBetweenObjects.x * j, 0, distanceBetweenObjects.y * i * yDirection);
                healthObject.transform.localRotation = Quaternion.identity;
                healthObject.transform.localScale = Vector3.one * healthObjectScale;
                healthObjects.Add(healthObject.transform);
                healthObject.SetActive(false);
            }
        }
        SetupHealthAndDamageTexts(owner, maxHealth, startPos, parent, yDirection);
    }

    private void SetupHealthAndDamageTexts(CardOwner owner, int maxHealth, Vector3 startPos, Transform parent, int yDirection)
    {
        healthTexts[(int)owner] = Instantiate(healthTextPrefab, Vector3.zero, Quaternion.identity, parent).GetComponent<TMPro.TextMeshPro>();
        damageTexts[(int)owner] = healthTexts[(int)owner].transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();
        damageTexts[(int)owner].text = "-0";
        damageTexts[(int)owner].gameObject.SetActive(false);
        healthTexts[(int)owner].text = maxHealth.ToString();
        if (owner == CardOwner.Player)
        {
            healthTexts[0].transform.localPosition = startPos + new Vector3(((maxHealth / (2f * maxColumns)) + 1) * distanceBetweenObjects.x, 0, distanceBetweenObjects.y * -1);
        }
        else
        {
            healthTexts[1].transform.localPosition = startPos + new Vector3(((maxHealth / (2f * maxColumns)) + 1) * distanceBetweenObjects.x, 0, distanceBetweenObjects.y * (maxHealth / maxColumns + 1) * -1);
        }
        healthTexts[(int)owner].transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        healthTexts[(int)owner].gameObject.SetActive(false);
    }

    public void ShowHealthObjects(bool show)
    {
        if (show)
        {
            for (int i = 0; i < playerHealthObjects.Count; i++)
            {
                playerHealthObjects[i].gameObject.SetActive(true);
                playerHealthObjects[i].DOScale(healthObjectScale, 1f).From(0f).SetEase(Ease.OutBack).SetDelay(i * 0.1f);
                playerHealthObjects[i].DOLocalMoveY(0f, 1f).From(-0.2f).SetEase(Ease.OutBack).SetDelay(i * 0.1f);
            }
            for (int i = 0; i < enemyHealthObjects.Count; i++)
            {
                AudioManager.instance.Play(SoundType.BottleMove);
                enemyHealthObjects[i].gameObject.SetActive(true);
                enemyHealthObjects[i].DOScale(healthObjectScale, 1f).From(0f).SetEase(Ease.OutBack).SetDelay(i * 0.1f);
                enemyHealthObjects[i].DOLocalMoveY(0f, 1f).From(-0.2f).SetEase(Ease.OutBack).SetDelay(i * 0.1f);
            }
            healthTexts[0].transform.DOScale(1f, 0f).SetDelay(playerHealthObjects.Count * 0.1f).OnComplete(() => healthTexts[0].gameObject.SetActive(true));
            healthTexts[1].transform.DOScale(1f, 0f).SetDelay(enemyHealthObjects.Count * 0.1f).OnComplete(() => healthTexts[1].gameObject.SetActive(true));
        }
    }

    public void TransferHealthObject(CardOwner ownerThatTookDamage, int damage)
    {
        List<Transform> sourceHealthObjects;
        List<Transform> targetHealthObjects;
        Vector3 startPos;
        int yDirection;
        int sourceIndex = 0;
        int targetIndex = 0;
        if (ownerThatTookDamage == CardOwner.Player)
        {
            sourceIndex = 0;
            targetIndex = 1;
            sourceHealthObjects = playerHealthObjects;
            targetHealthObjects = enemyHealthObjects;
            startPos = enemyHealthObjectStartPos;
            yDirection = -1;
        }
        else
        {
            sourceIndex = 1;
            targetIndex = 0;
            sourceHealthObjects = enemyHealthObjects;
            targetHealthObjects = playerHealthObjects;
            startPos = playerHealthObjectStartPos;
            yDirection = 1;
        }
        Debug.Log("Damage: " + damage);
        for (int i = 0; i < damage; i++)
        {
            Debug.Log("Source health objects count: " + sourceHealthObjects.Count);
            var sourceHealthObject = sourceHealthObjects[sourceHealthObjects.Count - 1];
            sourceHealthObjects.RemoveAt(sourceHealthObjects.Count - 1);
            int targetObjectIndex = targetHealthObjects.Count;
            int rowIndex = targetObjectIndex / maxColumns;
            int columnIndex = targetObjectIndex % maxColumns;
            var targetPosition = startPos + new Vector3(distanceBetweenObjects.x * columnIndex, 0, distanceBetweenObjects.y * rowIndex * yDirection);
            sourceHealthObject.DOLocalMove(targetPosition, 0.5f).SetEase(Ease.InCirc);
            healthTexts[sourceIndex].text = (int.Parse(healthTexts[sourceIndex].text) - 1).ToString();
            healthTexts[targetIndex].text = (int.Parse(healthTexts[targetIndex].text) + 1).ToString();

            healthTexts[0].transform.DOLocalMoveZ(playerHealthObjectStartPos.z + distanceBetweenObjects.y * -1, 0.3f).SetDelay(0.2f);
            healthTexts[1].transform.DOLocalMoveZ(enemyHealthObjectStartPos.z + distanceBetweenObjects.y * (enemyHealthObjects.Count / maxColumns + 1) * -1, 0.3f).SetDelay(0.2f);
            healthTexts[0].transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), 0.3f, 1, 0.5f).SetDelay(0.2f);
            healthTexts[1].transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), 0.3f, 1, 0.5f).SetDelay(0.2f);
            targetHealthObjects.Add(sourceHealthObject);

        }
        AudioManager.instance.Play(SoundType.BottleMove);
    }

    public void ResetDamageTexts()
    {
        damageTexts[0].text = "0";
        damageTexts[1].text = "0";
        PlayerManager.instance.OpponentCurrentDamage = 0;
        PlayerManager.instance.PlayerCurrentDamage = 0;
        damageTexts[0].gameObject.SetActive(false);
        damageTexts[1].gameObject.SetActive(false);
    }

    public void UpdateDamageTexts(CardOwner owner, int damage)
    {
        if (damage == 0)
        {
            damageTexts[(int)owner].gameObject.SetActive(false);
            return;
        }
        damageTexts[(int)owner].gameObject.SetActive(true);

        damageTexts[(int)owner].text = "-" + damage.ToString();
    }

}
