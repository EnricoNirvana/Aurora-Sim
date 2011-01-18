/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using OpenMetaverse;
using log4net;
using Nini.Config;
using System.Reflection;
using OpenSim.Services.Interfaces;
using OpenSim.Data;
using OpenSim.Framework;
using Aurora.Framework;
using Aurora.Simulation.Base;

namespace OpenSim.Services.InventoryService
{
    public class XInventoryService : IInventoryService, IService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        protected IXInventoryData m_Database;
        protected IUserAccountService m_UserAccountService;
        protected IAssetService m_AssetService;
        protected ILibraryService m_LibraryService;
		protected bool m_AllowDelete = true;

        public virtual void Initialize(IConfigSource config, IRegistryCore registry)
        {
            string dllName = String.Empty;
            string connString = String.Empty;
            //string realm = "Inventory"; // OSG version doesn't use this

            //
            // Try reading the [InventoryService] section first, if it exists
            //
            IConfig authConfig = config.Configs["InventoryService"];
            if (authConfig != null)
            {
                dllName = authConfig.GetString("StorageProvider", dllName);
                connString = authConfig.GetString("ConnectionString", connString);
                m_AllowDelete = authConfig.GetBoolean("AllowDelete", true);
                // realm = authConfig.GetString("Realm", realm);
            }

            //
            // Try reading the [DatabaseService] section, if it exists
            //
            IConfig dbConfig = config.Configs["DatabaseService"];
            if (dbConfig != null)
            {
                if (dllName == String.Empty)
                    dllName = dbConfig.GetString("StorageProvider", String.Empty);
                if (connString == String.Empty)
                    connString = dbConfig.GetString("ConnectionString", String.Empty);
            }

            //
            // We tried, but this doesn't exist. We can't proceed.
            //
            if (dllName == String.Empty)
                throw new Exception("No StorageProvider configured");

            m_Database = AuroraModuleLoader.LoadPlugin<IXInventoryData>(dllName,
                    new Object[] {connString, String.Empty});
            if (m_Database == null)
                throw new Exception("Could not find a storage interface in the given module");
            registry.RegisterModuleInterface<IInventoryService>(this);
        }

        public virtual void PostInitialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public virtual void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public virtual void PostStart(IConfigSource config, IRegistryCore registry)
        {
            m_UserAccountService = registry.RequestModuleInterface<IUserAccountService>();
            m_LibraryService = registry.RequestModuleInterface<ILibraryService>();
            m_AssetService = registry.RequestModuleInterface<IAssetService>();
        }

        public void AddNewRegistry(IConfigSource config, IRegistryCore registry)
        {
        }

        public virtual bool CreateUserInventory(UUID principalID)
        {
            // This is braindeaad. We can't ever communicate that we fixed
            // an existing inventory. Well, just return root folder status,
            // but check sanity anyway.
            //
            bool result = false;

            InventoryFolderBase rootFolder = GetRootFolder(principalID);

            if (rootFolder == null)
            {
                rootFolder = ConvertToOpenSim(CreateFolder(principalID, UUID.Zero, (int)AssetType.RootFolder, "My Inventory"));
                result = true;
            }

            XInventoryFolder[] sysFolders = GetSystemFolders(principalID);

            if (!Array.Exists(sysFolders, delegate (XInventoryFolder f) { if (f.type == (int)AssetType.Animation) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Animation, "Animations");
            if (!Array.Exists(sysFolders, delegate (XInventoryFolder f) { if (f.type == (int)AssetType.Bodypart) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Bodypart, "Body Parts");
            if (!Array.Exists(sysFolders, delegate (XInventoryFolder f) { if (f.type == (int)AssetType.CallingCard) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.CallingCard, "Calling Cards");
            if (!Array.Exists(sysFolders, delegate (XInventoryFolder f) { if (f.type == (int)AssetType.Clothing) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Clothing, "Clothing");
            if (!Array.Exists(sysFolders, delegate (XInventoryFolder f) { if (f.type == (int)AssetType.Gesture) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Gesture, "Gestures");
            if (!Array.Exists(sysFolders, delegate (XInventoryFolder f) { if (f.type == (int)AssetType.Landmark) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Landmark, "Landmarks");
            if (!Array.Exists(sysFolders, delegate (XInventoryFolder f) { if (f.type == (int)AssetType.LostAndFoundFolder) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.LostAndFoundFolder, "Lost And Found");
            if (!Array.Exists(sysFolders, delegate (XInventoryFolder f) { if (f.type == (int)AssetType.Notecard) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Notecard, "Notecards");
            if (!Array.Exists(sysFolders, delegate (XInventoryFolder f) { if (f.type == (int)AssetType.Object) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Object, "Objects");
            if (!Array.Exists(sysFolders, delegate (XInventoryFolder f) { if (f.type == (int)AssetType.SnapshotFolder) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.SnapshotFolder, "Photo Album");
            if (!Array.Exists(sysFolders, delegate (XInventoryFolder f) { if (f.type == (int)AssetType.LSLText) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.LSLText, "Scripts");
            if (!Array.Exists(sysFolders, delegate (XInventoryFolder f) { if (f.type == (int)AssetType.Sound) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Sound, "Sounds");
            if (!Array.Exists(sysFolders, delegate (XInventoryFolder f) { if (f.type == (int)AssetType.Texture) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Texture, "Textures");
            if (!Array.Exists(sysFolders, delegate (XInventoryFolder f) { if (f.type == (int)AssetType.TrashFolder) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.TrashFolder, "Trash");

            if (m_LibraryService != null)
            {

                InventoryFolderBase bodypartFolder = GetFolderForType(principalID, AssetType.Bodypart);
                InventoryFolderBase clothingFolder = GetFolderForType(principalID, AssetType.Clothing);

                // Default items
                InventoryItemBase defaultShape = new InventoryItemBase();
                defaultShape.Name = "Default shape";
                defaultShape.Description = "Default shape description";
                defaultShape.AssetType = (int)AssetType.Bodypart;
                defaultShape.InvType = (int)InventoryType.Wearable;
                defaultShape.Flags = (uint)WearableType.Shape;
                defaultShape.ID = AvatarWearable.DEFAULT_BODY_ITEM;
                //Give a new copy to every person
                AssetBase asset = m_AssetService.Get(AvatarWearable.DEFAULT_BODY_ASSET.ToString());
                asset.FullID = UUID.Random();
                m_AssetService.Store(asset);
                defaultShape.AssetID = asset.FullID;
                defaultShape.Folder = bodypartFolder.ID;
                defaultShape.CreatorId = UUID.Zero.ToString();
                AddItem(defaultShape);

                InventoryItemBase defaultSkin = new InventoryItemBase();
                defaultSkin.Name = "Default skin";
                defaultSkin.Description = "Default skin description";
                defaultSkin.AssetType = (int)AssetType.Bodypart;
                defaultSkin.InvType = (int)InventoryType.Wearable;
                defaultSkin.Flags = (uint)WearableType.Skin;
                defaultSkin.ID = AvatarWearable.DEFAULT_SKIN_ITEM;
                //Give a new copy to every person
                asset = m_AssetService.Get(AvatarWearable.DEFAULT_SKIN_ASSET.ToString());
                asset.FullID = UUID.Random();
                m_AssetService.Store(asset);
                defaultSkin.AssetID = asset.FullID;
                defaultSkin.Folder = bodypartFolder.ID;
                defaultSkin.CreatorId = m_LibraryService.LibraryOwner.ToString();
                defaultSkin.Owner = principalID;
                defaultSkin.BasePermissions = (uint)PermissionMask.All;
                defaultSkin.CurrentPermissions = (uint)PermissionMask.All;
                defaultSkin.EveryOnePermissions = (uint)PermissionMask.None;
                defaultSkin.NextPermissions = (uint)PermissionMask.All;
                AddItem(defaultSkin);

                InventoryItemBase defaultHair = new InventoryItemBase();
                defaultHair.Name = "Default hair";
                defaultHair.Description = "Default hair description";
                defaultHair.AssetType = (int)AssetType.Bodypart;
                defaultHair.InvType = (int)InventoryType.Wearable;
                defaultHair.Flags = (uint)WearableType.Hair;
                defaultHair.ID = AvatarWearable.DEFAULT_HAIR_ITEM;
                //Give a new copy to every person
                asset = m_AssetService.Get(AvatarWearable.DEFAULT_HAIR_ASSET.ToString());
                asset.FullID = UUID.Random();
                m_AssetService.Store(asset);
                defaultHair.AssetID = asset.FullID;
                defaultHair.Folder = bodypartFolder.ID;
                defaultHair.CreatorId = m_LibraryService.LibraryOwner.ToString();
                defaultHair.Owner = principalID;
                defaultHair.BasePermissions = (uint)PermissionMask.All;
                defaultHair.CurrentPermissions = (uint)PermissionMask.All;
                defaultHair.EveryOnePermissions = (uint)PermissionMask.None;
                defaultHair.NextPermissions = (uint)PermissionMask.All;
                AddItem(defaultHair);

                InventoryItemBase defaultEyes = new InventoryItemBase();
                defaultEyes.Name = "Default eyes";
                defaultEyes.Description = "Default eyes description";
                defaultEyes.AssetType = (int)AssetType.Bodypart;
                defaultEyes.InvType = (int)InventoryType.Wearable;
                defaultEyes.Flags = (uint)WearableType.Eyes;
                defaultEyes.ID = AvatarWearable.DEFAULT_EYES_ITEM;
                //Give a new copy to every person
                asset = m_AssetService.Get(AvatarWearable.DEFAULT_EYES_ASSET.ToString());
                if (asset == null)
                {
                    asset = new AssetBase(AvatarWearable.DEFAULT_EYES_ASSET.ToString(),
                        "Eyes", 13, m_LibraryService.LibraryOwner.ToString());
                    asset.Data = Util.StringToBytes256(@"LLWearable version 22
New Eyes

	permissions 0
	{
		base_mask	7fffffff
		owner_mask	7fffffff
		group_mask	00000000
		everyone_mask	00000000
		next_owner_mask	00082000
		creator_id	22435ff6-ce10-4165-9973-bba745b84a21
		owner_id	22435ff6-ce10-4165-9973-bba745b84a21
		last_owner_id	00000000-0000-0000-0000-000000000000
		group_id	00000000-0000-0000-0000-000000000000
	}
	sale_info	0
	{
		sale_type	not
		sale_price	10
	}
type 3
parameters 2
98 0
99 0
textures 1
3 6522e74d-1660-4e7f-b601-6f48c1659a77");
                    m_AssetService.Store(asset);
                }
                asset.FullID = UUID.Random();
                m_AssetService.Store(asset);
                defaultEyes.AssetID = asset.FullID;
                defaultEyes.Folder = bodypartFolder.ID;
                defaultEyes.CreatorId = m_LibraryService.LibraryOwner.ToString();
                defaultEyes.Owner = principalID;
                defaultEyes.BasePermissions = (uint)PermissionMask.All;
                defaultEyes.CurrentPermissions = (uint)PermissionMask.All;
                defaultEyes.EveryOnePermissions = (uint)PermissionMask.None;
                defaultEyes.NextPermissions = (uint)PermissionMask.All;
                AddItem(defaultEyes);

                InventoryItemBase defaultShirt = new InventoryItemBase();
                defaultShirt.Name = "Default shirt";
                defaultShirt.Description = "Default shirt description";
                defaultShirt.AssetType = (int)AssetType.Clothing;
                defaultShirt.InvType = (int)InventoryType.Wearable;
                defaultShirt.Flags = (uint)WearableType.Shirt;
                defaultShirt.ID = AvatarWearable.DEFAULT_SHIRT_ITEM;
                //Give a new copy to every person
                asset = m_AssetService.Get(AvatarWearable.DEFAULT_SHIRT_ASSET.ToString());
                asset.FullID = UUID.Random();
                m_AssetService.Store(asset);
                defaultShirt.AssetID = asset.FullID;
                defaultShirt.Folder = clothingFolder.ID;
                defaultShirt.CreatorId = m_LibraryService.LibraryOwner.ToString();
                defaultShirt.Owner = principalID;
                defaultShirt.BasePermissions = (uint)PermissionMask.All;
                defaultShirt.CurrentPermissions = (uint)PermissionMask.All;
                defaultShirt.EveryOnePermissions = (uint)PermissionMask.None;
                defaultShirt.NextPermissions = (uint)PermissionMask.All;
                AddItem(defaultShirt);

                InventoryItemBase defaultPants = new InventoryItemBase();
                defaultPants.Name = "Default pants";
                defaultPants.Description = "Default pants description";
                defaultPants.AssetType = (int)AssetType.Clothing;
                defaultPants.InvType = (int)InventoryType.Wearable;
                defaultPants.Flags = (uint)WearableType.Shirt;
                defaultPants.ID = AvatarWearable.DEFAULT_PANTS_ITEM;
                //Give a new copy to every person
                asset = m_AssetService.Get(AvatarWearable.DEFAULT_PANTS_ASSET.ToString());
                asset.FullID = UUID.Random();
                m_AssetService.Store(asset);
                defaultPants.AssetID = asset.FullID;
                defaultPants.Folder = clothingFolder.ID;
                defaultPants.CreatorId = m_LibraryService.LibraryOwner.ToString();
                defaultPants.Owner = principalID;
                defaultPants.BasePermissions = (uint)PermissionMask.All;
                defaultPants.CurrentPermissions = (uint)PermissionMask.All;
                defaultPants.EveryOnePermissions = (uint)PermissionMask.None;
                defaultPants.NextPermissions = (uint)PermissionMask.All;
                AddItem(defaultPants);
            }

            return result;
        }

        protected XInventoryFolder CreateFolder(UUID principalID, UUID parentID, int type, string name)
        {
            XInventoryFolder newFolder = new XInventoryFolder();

            newFolder.folderName = name;
            newFolder.type = type;
            newFolder.version = 1;
            newFolder.folderID = UUID.Random();
            newFolder.agentID = principalID;
            newFolder.parentFolderID = parentID;

            m_Database.StoreFolder(newFolder);

            return newFolder;
        }

        protected virtual XInventoryFolder[] GetSystemFolders(UUID principalID)
        {
//            m_log.DebugFormat("[XINVENTORY SERVICE]: Getting system folders for {0}", principalID);
            
            XInventoryFolder[] allFolders = m_Database.GetFolders(
                    new string[] { "agentID" },
                    new string[] { principalID.ToString() });

            XInventoryFolder[] sysFolders = Array.FindAll(
                    allFolders,
                    delegate (XInventoryFolder f)
                    {
                        if (f.type > 0)
                            return true;
                        return false;
                    });

//            m_log.DebugFormat(
//                "[XINVENTORY SERVICE]: Found {0} system folders for {1}", sysFolders.Length, principalID);
            
            return sysFolders;
        }

        public virtual List<InventoryFolderBase> GetInventorySkeleton(UUID principalID)
        {
            XInventoryFolder[] allFolders = m_Database.GetFolders(
                    new string[] { "agentID" },
                    new string[] { principalID.ToString() });

            if (allFolders.Length == 0)
                return null;

            List<InventoryFolderBase> folders = new List<InventoryFolderBase>();

            foreach (XInventoryFolder x in allFolders)
            {
                //m_log.DebugFormat("[XINVENTORY SERVICE]: Adding folder {0} to skeleton", x.folderName);
                folders.Add(ConvertToOpenSim(x));
            }

            return folders;
        }

        public virtual InventoryFolderBase GetRootFolder(UUID principalID)
        {
            XInventoryFolder[] folders = m_Database.GetFolders(
                    new string[] { "agentID", "parentFolderID"},
                    new string[] { principalID.ToString(), UUID.Zero.ToString() });

            if (folders.Length == 0)
                return null;

            XInventoryFolder root = null;
            foreach (XInventoryFolder folder in folders)
                if (folder.folderName == "My Inventory")
                    root = folder;
            if (folders == null) // oops
                root = folders[0];

            return ConvertToOpenSim(root);
        }

        public virtual InventoryFolderBase GetFolderForType(UUID principalID, AssetType type)
        {
//            m_log.DebugFormat("[XINVENTORY SERVICE]: Getting folder type {0} for user {1}", type, principalID);
            
            XInventoryFolder[] folders = m_Database.GetFolders(
                    new string[] { "agentID", "type"},
                    new string[] { principalID.ToString(), ((int)type).ToString() });

            if (folders.Length == 0)
            {
//                m_log.WarnFormat("[XINVENTORY SERVICE]: Found no folder for type {0} for user {1}", type, principalID);
                return null;
            }
            
//            m_log.DebugFormat(
//                "[XINVENTORY SERVICE]: Found folder {0} {1} for type {2} for user {3}", 
//                folders[0].folderName, folders[0].folderID, type, principalID);

            return ConvertToOpenSim(folders[0]);
        }

        public virtual InventoryCollection GetFolderContent(UUID principalID, UUID folderID)
        {
            // This method doesn't receive a valud principal id from the
            // connector. So we disregard the principal and look
            // by ID.
            //
            m_log.DebugFormat("[XINVENTORY SERVICE]: Fetch contents for folder {0}", folderID.ToString());
            InventoryCollection inventory = new InventoryCollection();
            inventory.UserID = principalID;
            inventory.Folders = new List<InventoryFolderBase>();
            inventory.Items = new List<InventoryItemBase>();

            XInventoryFolder[] folders = m_Database.GetFolders(
                    new string[] { "parentFolderID"},
                    new string[] { folderID.ToString() });

            foreach (XInventoryFolder x in folders)
            {
                //m_log.DebugFormat("[XINVENTORY]: Adding folder {0} to response", x.folderName);
                inventory.Folders.Add(ConvertToOpenSim(x));
            }

            XInventoryItem[] items = m_Database.GetItems(
                    new string[] { "parentFolderID"},
                    new string[] { folderID.ToString() });

            foreach (XInventoryItem i in items)
            {
                //m_log.DebugFormat("[XINVENTORY]: Adding item {0} to response", i.inventoryName);
                inventory.Items.Add(ConvertToOpenSim(i));
            }

            return inventory;
        }
        
        public virtual List<InventoryItemBase> GetFolderItems(UUID principalID, UUID folderID)
        {
//            m_log.DebugFormat("[XINVENTORY]: Fetch items for folder {0}", folderID);
            
            // Since we probably don't get a valid principal here, either ...
            //
            List<InventoryItemBase> invItems = new List<InventoryItemBase>();

            XInventoryItem[] items = m_Database.GetItems(
                    new string[] { "parentFolderID"},
                    new string[] { folderID.ToString() });

            foreach (XInventoryItem i in items)
                invItems.Add(ConvertToOpenSim(i));

            return invItems;
        }

        public virtual bool AddFolder(InventoryFolderBase folder)
        {
            InventoryFolderBase check = GetFolder(folder);
            if (check != null)
                return false;

            XInventoryFolder xFolder = ConvertFromOpenSim(folder);
            return m_Database.StoreFolder(xFolder);
        }

        public virtual bool UpdateFolder(InventoryFolderBase folder)
        {
            XInventoryFolder xFolder = ConvertFromOpenSim(folder);
            InventoryFolderBase check = GetFolder(folder);
            if (check == null)
                return AddFolder(folder);

            if (check.Type != -1 || xFolder.type != -1)
            {
                if (xFolder.version > check.Version)
                    return false;
                check.Version = (ushort)xFolder.version;
                xFolder = ConvertFromOpenSim(check);
                return m_Database.StoreFolder(xFolder);
            }

            if (xFolder.version < check.Version)
                xFolder.version = check.Version;
            xFolder.folderID = check.ID;

            return m_Database.StoreFolder(xFolder);
        }

        public virtual bool MoveFolder(InventoryFolderBase folder)
        {
            XInventoryFolder[] x = m_Database.GetFolders(
                    new string[] { "folderID" },
                    new string[] { folder.ID.ToString() });

            if (x.Length == 0)
                return false;

            x[0].parentFolderID = folder.ParentID;

            return m_Database.StoreFolder(x[0]);
        }

        // We don't check the principal's ID here
        //
        public virtual bool DeleteFolders(UUID principalID, List<UUID> folderIDs)
        {
            if (!m_AllowDelete)
                return false;

            // Ignore principal ID, it's bogus at connector level
            //
            foreach (UUID id in folderIDs)
            {
                if (!ParentIsTrash(id))
                    continue;
                InventoryFolderBase f = new InventoryFolderBase();
                f.ID = id;
                PurgeFolder(f);
                m_Database.DeleteFolders("folderID", id.ToString());
            }

            return true;
        }

        public virtual bool PurgeFolder(InventoryFolderBase folder)
        {
            if (!m_AllowDelete)
                return false;

            if (!ParentIsTrash(folder.ID))
                return false;

            XInventoryFolder[] subFolders = m_Database.GetFolders(
                    new string[] { "parentFolderID" },
                    new string[] { folder.ID.ToString() });

            foreach (XInventoryFolder x in subFolders)
            {
                PurgeFolder(ConvertToOpenSim(x));
                m_Database.DeleteFolders("folderID", x.folderID.ToString());
            }

            m_Database.DeleteItems("parentFolderID", folder.ID.ToString());

            return true;
        }

        public virtual bool AddItem(InventoryItemBase item)
        {
//            m_log.DebugFormat(
//                "[XINVENTORY SERVICE]: Adding item {0} to folder {1} for {2}", item.ID, item.Folder, item.Owner);
            
            return m_Database.StoreItem(ConvertFromOpenSim(item));
        }

        public virtual bool UpdateItem(InventoryItemBase item)
        {
            return m_Database.StoreItem(ConvertFromOpenSim(item));
        }

        public virtual bool MoveItems(UUID principalID, List<InventoryItemBase> items)
        {
            // Principal is b0rked. *sigh*
            //
            foreach (InventoryItemBase i in items)
            {
                m_Database.MoveItem(i.ID.ToString(), i.Folder.ToString());
            }

            return true;
        }

        public virtual bool DeleteItems(UUID principalID, List<UUID> itemIDs)
        {
            if (!m_AllowDelete)
                return false;

            // Just use the ID... *facepalms*
            //
            foreach (UUID id in itemIDs)
                m_Database.DeleteItems("inventoryID", id.ToString());

            return true;
        }

        public virtual InventoryItemBase GetItem(InventoryItemBase item)
        {
            XInventoryItem[] items = m_Database.GetItems(
                    new string[] { "inventoryID" },
                    new string[] { item.ID.ToString() });

            foreach (XInventoryItem xitem in items)
            {
                UUID nn;
                if (!UUID.TryParse(xitem.creatorID, out nn))
                {
                    try
                    {
                        if (xitem.creatorID != string.Empty)
                        {
                            string FullName = xitem.creatorID.Remove(0, 7);
                            string[] FirstLast = FullName.Split(' ');
                            UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, FirstLast[0], FirstLast[1]);
                            if (account == null)
                            {
                                xitem.creatorID = UUID.Zero.ToString();
                                m_Database.StoreItem(xitem);
                            }
                            else
                            {
                                xitem.creatorID = account.PrincipalID.ToString();
                                m_Database.StoreItem(xitem);
                            }
                        }
                        else
                        {
                            xitem.creatorID = UUID.Zero.ToString();
                            m_Database.StoreItem(xitem);
                        }
                    }
                    catch
                    {
                        xitem.creatorID = UUID.Zero.ToString();
                    }
                }
            }

            if (items.Length == 0)
                return null;

            return ConvertToOpenSim(items[0]);
        }

        public virtual InventoryFolderBase GetFolder(InventoryFolderBase folder)
        {
            XInventoryFolder[] folders = m_Database.GetFolders(
                    new string[] { "folderID"},
                    new string[] { folder.ID.ToString() });

            if (folders.Length == 0)
                return null;

            return ConvertToOpenSim(folders[0]);
        }

        public virtual List<InventoryItemBase> GetActiveGestures(UUID principalID)
        {
            XInventoryItem[] items = m_Database.GetActiveGestures(principalID);

            if (items.Length == 0)
                return new List<InventoryItemBase>();

            List<InventoryItemBase> ret = new List<InventoryItemBase>();
            
            foreach (XInventoryItem x in items)
                ret.Add(ConvertToOpenSim(x));

            return ret;
        }

        public virtual int GetAssetPermissions(UUID principalID, UUID assetID)
        {
            return m_Database.GetAssetPermissions(principalID, assetID);
        }

        // Unused.
        //
        public bool HasInventoryForUser(UUID userID)
        {
            return false;
        }

        // CM Helpers
        //
        protected InventoryFolderBase ConvertToOpenSim(XInventoryFolder folder)
        {
            InventoryFolderBase newFolder = new InventoryFolderBase();

            newFolder.ParentID = folder.parentFolderID;
            newFolder.Type = (short)folder.type;
            newFolder.Version = (ushort)folder.version;
            newFolder.Name = folder.folderName;
            newFolder.Owner = folder.agentID;
            newFolder.ID = folder.folderID;

            return newFolder;
        }

        protected XInventoryFolder ConvertFromOpenSim(InventoryFolderBase folder)
        {
            XInventoryFolder newFolder = new XInventoryFolder();

            newFolder.parentFolderID = folder.ParentID;
            newFolder.type = (int)folder.Type;
            newFolder.version = (int)folder.Version;
            newFolder.folderName = folder.Name;
            newFolder.agentID = folder.Owner;
            newFolder.folderID = folder.ID;

            return newFolder;
        }

        protected InventoryItemBase ConvertToOpenSim(XInventoryItem item)
        {
            InventoryItemBase newItem = new InventoryItemBase();

            newItem.AssetID = item.assetID;
            newItem.AssetType = item.assetType;
            newItem.Name = item.inventoryName;
            newItem.Owner = item.avatarID;
            newItem.ID = item.inventoryID;
            newItem.InvType = item.invType;
            newItem.Folder = item.parentFolderID;
            newItem.CreatorId = item.creatorID;
            newItem.Description = item.inventoryDescription;
            newItem.NextPermissions = (uint)item.inventoryNextPermissions;
            newItem.CurrentPermissions = (uint)item.inventoryCurrentPermissions;
            newItem.BasePermissions = (uint)item.inventoryBasePermissions;
            newItem.EveryOnePermissions = (uint)item.inventoryEveryOnePermissions;
            newItem.GroupPermissions = (uint)item.inventoryGroupPermissions;
            newItem.GroupID = item.groupID;
            if (item.groupOwned == 0)
                newItem.GroupOwned = false;
            else
                newItem.GroupOwned = true;
            newItem.SalePrice = item.salePrice;
            newItem.SaleType = (byte)item.saleType;
            newItem.Flags = (uint)item.flags;
            newItem.CreationDate = item.creationDate;

            return newItem;
        }

        protected XInventoryItem ConvertFromOpenSim(InventoryItemBase item)
        {
            XInventoryItem newItem = new XInventoryItem();

            newItem.assetID = item.AssetID;
            newItem.assetType = item.AssetType;
            newItem.inventoryName = item.Name;
            newItem.avatarID = item.Owner;
            newItem.inventoryID = item.ID;
            newItem.invType = item.InvType;
            newItem.parentFolderID = item.Folder;
            newItem.creatorID = item.CreatorId;
            newItem.inventoryDescription = item.Description;
            newItem.inventoryNextPermissions = (int)item.NextPermissions;
            newItem.inventoryCurrentPermissions = (int)item.CurrentPermissions;
            newItem.inventoryBasePermissions = (int)item.BasePermissions;
            newItem.inventoryEveryOnePermissions = (int)item.EveryOnePermissions;
            newItem.inventoryGroupPermissions = (int)item.GroupPermissions;
            newItem.groupID = item.GroupID;
            if (item.GroupOwned)
                newItem.groupOwned = 1;
            else
                newItem.groupOwned = 0;
            newItem.salePrice = item.SalePrice;
            newItem.saleType = (int)item.SaleType;
            newItem.flags = (int)item.Flags;
            newItem.creationDate = item.CreationDate;

            return newItem;
        }

        private bool ParentIsTrash(UUID folderID)
        {
            XInventoryFolder[] folder = m_Database.GetFolders(new string[] {"folderID"}, new string[] {folderID.ToString()});
            if (folder.Length < 1)
                return false;

            if (folder[0].type == (int)AssetType.TrashFolder)
                return true;

            UUID parentFolder = folder[0].parentFolderID;

            while (parentFolder != UUID.Zero)
            {
                XInventoryFolder[] parent = m_Database.GetFolders(new string[] {"folderID"}, new string[] {parentFolder.ToString()});
                if (parent.Length < 1)
                    return false;

                if (parent[0].type == (int)AssetType.TrashFolder)
                    return true;
                if (parent[0].type == (int)AssetType.RootFolder)
                    return false;

                parentFolder = parent[0].parentFolderID;
            }
            return false;
        }
    }
}
