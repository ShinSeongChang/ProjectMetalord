using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 수집 아이템 스크립트
/// 231219 배경택
/// </summary>
public class RecordItem : MonoBehaviour
{
    [SerializeField] RecordList recordItem;

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            GameEventsManager.instance.recordEvents.GetRecordItem((int)recordItem);
            Debug.Log($"수집품 ID : {(int)recordItem}, 이름 : {recordItem}");
            Destroy(this.gameObject);
        }
    }
}