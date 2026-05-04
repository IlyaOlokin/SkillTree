mergeInto(LibraryManager.library, {
  SkillTreeSyncPersistentDataPath: function () {
    if (typeof FS === "undefined" || typeof FS.syncfs !== "function") {
      return;
    }

    if (Module.SkillTreePersistentSyncInProgress) {
      Module.SkillTreePersistentSyncPending = true;
      return;
    }

    var sync = function () {
      Module.SkillTreePersistentSyncInProgress = true;
      Module.SkillTreePersistentSyncPending = false;

      FS.syncfs(false, function (error) {
        if (error) {
          console.error("SkillTree persistent data sync failed:", error);
        }

        Module.SkillTreePersistentSyncInProgress = false;
        if (Module.SkillTreePersistentSyncPending) {
          sync();
        }
      });
    };

    sync();
  }
});
