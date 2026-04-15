using System.Collections.Generic;
using UnityEngine;


public class AccountUser
{
    public string Id;
    public string DisplayName;

    public AccountUser() { }
    public AccountUser(string id) { Id = id; DisplayName = id; }
}