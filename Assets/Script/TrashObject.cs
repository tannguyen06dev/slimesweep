using UnityEngine;
using Photon.Pun;

public class TrashObject : MonoBehaviourPun
{
    public int ownerActorNr = -1; // -1 nghĩa là chưa có owner

    [PunRPC]
    public void SetOwner(int actorNr)
    {
        ownerActorNr = actorNr;
        Debug.Log($"Rác {gameObject.name} được set owner: {actorNr}");
    }
}