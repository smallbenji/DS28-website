<script lang="ts" setup>
import { ref, computed } from 'vue';
import { useGroupStore } from '@/Stores/GroupStore';
import { storeToRefs } from 'pinia';

const groupStore = useGroupStore();
const { Groups: groups } = storeToRefs(groupStore);

const selectedGroup = ref<DSGroup | null>(null);
const isCreating = ref(false);
const newGroup = ref<DSGroup>({ id: 0, name: '' });

const searchQuery = ref('');

const filteredGroups = computed(() => {
  if (!searchQuery.value.trim()) return groups.value;

  const query = searchQuery.value.toLowerCase();
  return groups.value.filter(g => {
    return g.name.toLowerCase().includes(query) || g.id.toString().toLowerCase().includes(query);
  });
});

const toggleGroupSelection = (clickedGroup: DSGroup) => {
  isCreating.value = false;
  if (selectedGroup.value?.id === clickedGroup.id) {
    selectedGroup.value = null;
  } else {
    selectedGroup.value = JSON.parse(JSON.stringify(clickedGroup));
  }
};

const startCreateGroup = () => {
  selectedGroup.value = null;
  isCreating.value = true;
  newGroup.value = { id: 0, name: '' };
};

const saveChanges = async () => {
  if (isCreating.value) {
    if (!newGroup.value.id || !newGroup.value.name) {
      alert('Udfyld venligst både ID og navn');
      return;
    }
    const success = await groupStore.CREATE_GROUP(newGroup.value);
    if (success) {
      isCreating.value = false;
      selectedGroup.value = null;
    } else {
      alert('Der skete en fejl ved oprettelse af gruppen');
    }
  } else if (selectedGroup.value) {
    const success = await groupStore.UPDATE_GROUP(selectedGroup.value);
    if (success) {

    } else {
      alert('Der skete en fejl ved opdatering af gruppen');
    }
  }
};
</script>

<template>
    <div class="management-wrapper">
        <aside class="sidebar-list">
            <div class="list-header">
                <div class="title-row">
                    <h2>Grupper</h2>
                    <span class="count">{{ filteredGroups.length }} totalt</span>
                </div>

                <div class="search-container">
                    <input
                        v-model="searchQuery"
                        type="text"
                        placeholder="Søg på navn eller ID..."
                        class="search-input"
                    />
                </div>
            </div>

            <div class="scroll-area">
                <div
                    v-for="group in filteredGroups"
                    :key="group.id"
                    class="user-row"
                    :class="{ active: selectedGroup?.id === group.id }"
                    @click="toggleGroupSelection(group)"
                >
                    <div class="user-main">
                        <span class="full-name">{{ group.name }}</span>
                    </div>

                    <div class="group-info">
                        ID: {{ group.id }}
                    </div>
                </div>
            </div>

            <div class="sidebar-footer">
                <button @click="startCreateGroup" class="btn-add-user">
                    + Tilføj ny gruppe
                </button>
            </div>
        </aside>

        <main class="workspace">
            <div class="workspace-box" :class="(selectedGroup || isCreating) ? 'filled' : 'dashed'">
                <Transition name="slide-fade" mode="out-in">

                    <div v-if="isCreating" class="editor-surface">
                        <header class="editor-header">
                            <div class="title-section">
                                <h1>Ny Gruppe</h1>
                                <p>Opret en ny gruppe i systemet</p>
                            </div>
                            <div class="action-bar">
                                <button @click="saveChanges" class="btn-save">Opret gruppe</button>
                            </div>
                        </header>

                        <section class="form-layout">
                            <div class="form-card">
                                <h3>Gruppeoplysninger</h3>
                                <div class="grid-2">
                                    <div class="input-field">
                                        <label>ID</label>
                                        <input v-model.number="newGroup.id" type="number" placeholder="F.eks. 000001" />
                                    </div>
                                    <div class="input-field">
                                        <label>Navn</label>
                                        <input v-model="newGroup.name" type="text" placeholder="Gruppens navn" />
                                    </div>
                                </div>
                            </div>
                        </section>
                    </div>

                    <div v-else-if="selectedGroup" :key="selectedGroup.id" class="editor-surface">
                        <header class="editor-header">
                            <div class="title-section">
                                <h1>{{ selectedGroup.name }}</h1>
                                <p>Gruppe-ID: {{ selectedGroup.id }}</p>
                            </div>
                            <div class="action-bar">
                                <button @click="saveChanges" class="btn-save">Gem ændringer</button>
                            </div>
                        </header>

                        <section class="form-layout">
                            <div class="form-card">
                                <h3>Gruppeoplysninger</h3>
                                <div class="input-field">
                                    <label>Navn</label>
                                    <input v-model="selectedGroup.name" type="text" />
                                </div>
                            </div>

                            <!-- Plads til senere: stats og patruljer -->
                            <div class="form-card">
                                <h3>Kommende funktioner</h3>
                                <p style="color: #64748b; font-size: 0.9rem;">Her vil der senere være mulighed for at styre patruljer og se statistikker for gruppen.</p>
                            </div>
                        </section>
                    </div>

                    <div v-else class="empty-workspace">
                        <p>Vælg en gruppe fra listen til venstre for at starte redigering</p>
                    </div>

                </Transition>
            </div>
        </main>
    </div>
</template>

<style lang="scss" scoped>
.management-wrapper {
  display: flex;
  height: 100vh;
  width: 100%;
  background: #d4d4d4;

  .sidebar-list {
    width: 380px;
    border-right: 1px solid #e2e8f0;
    display: flex;
    flex-direction: column;
    background: #fff;
    margin: 1rem;
    margin-right: 0;
    border-radius: 10px;
    position: sticky;
    top: 0;

    .list-header {
      flex-shrink: 0;
      padding: 1.5rem 1.5rem 1rem 1.5rem;
      border-bottom: 1px solid #f1f5f9;

      .title-row {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 1rem;
        h2 { margin: 0; font-size: 1.5rem; color: #0f172a; }
        .count { color: #64748b; font-size: 0.8rem; }
      }

      .search-container {
        width: 100%;
        .search-input {
          width: 100%;
          padding: 0.6rem 0.8rem;
          border-radius: 8px;
          border: 1px solid #afafaf;
          font-size: 0.9rem;
          background-color: #ececec;
          box-sizing: border-box;
          transition: all 0.2s;

          &:focus {
            outline: none;
            border-color: rgb(88, 88, 88);
            background-color: #fff;
          }
        }
      }
    }

    .scroll-area {
      flex: 1;
      overflow-y: auto;
      padding: 1rem;

      &::-webkit-scrollbar {
        width: 6px;
      }

      &::-webkit-scrollbar-thumb {
        background: #e2e8f0;
        border-radius: 10px;
      }
    }

    .sidebar-footer {
      flex-shrink: 0;
      padding: 1rem 1.5rem;
      border-top: 1px solid #f1f5f9;

      .btn-add-user {
        width: 100%;
        padding: 0.75rem;
        border-radius: 8px;
        border: 2px dashed #a5a5a5;
        background-color: #fff;
        color: #7c7c7ca2;
        font-weight: 600;
        cursor: pointer;
        position: relative;
        transition: all 0.35s;
        z-index: 1;

        &::after {
          content: "";
          position: absolute;
          inset: -2px;
          border: 2px solid #838383;
          border-radius: 8px;
          opacity: 0;
          transition: opacity 0.35s ease;
          z-index: -1;
        }

        &:hover {
          color: #444444;
          background-color: #f0f0f0;

          &::after {
            opacity: 1;
          }
        }
      }
    }
  }

  .user-row {
    padding: 1rem 1.2rem;
    margin-bottom: 0.75rem;
    border-radius: 10px;
    border: 1px solid #c4c4c4;
    cursor: pointer;
    transition: all 0.2s ease;
    background: #fff;

    &:hover {
      background: #f8fafc;
      border-color: #a5a5a5;
    }

    &.active {
      background: #ececec;
      border: 1px solid #696969;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.02);

      .full-name { color: #0f172a; }
    }

    .user-main {
      margin-bottom: 0.4rem;
      .full-name { display: block; font-weight: 600; color: #334155; }
    }

    .group-info {
      font-size: 0.8rem;
      color: #8f8f8f;
    }
  }

  .workspace {
    flex: 1;
    display: flex;
    padding: 1rem;
    overflow-y: auto;

    .workspace-box {
      flex: 1;
      border-radius: 10px;
      transition: border 0.3s ease, background-color 0.3s ease, all 0.3s;
      display: flex;
      flex-direction: column;
      overflow-x: hidden;
      overflow-y: hidden;

      &.dashed {
        border: 2px dashed #00000026;
        background-color: transparent;
      }

      &.filled {
        border: 2px solid #aaaaaa;
        background-color: #ececec;
      }
    }
  }

  .editor-surface {
    padding: 3rem;
    width: 100%;
    max-width: 900px;
    margin: 0 auto;

    .editor-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 3rem;

      .title-section {
        h1 {
            margin: 0 0 0.5rem 0;
            font-size: 2rem;
            color: #0f172a;
        }

        p {
            margin: 0;
            color: #64748b;
            font-size: 0.85rem;
            font-family: monospace;
        }
      }
    }
  }

  .form-card {
    background: #fff;
    padding: 2rem;
    border-radius: 16px;
    border: 1px solid #b9b9b9;
    box-shadow: 0 1px 3px rgba(0,0,0,0.1);
    margin-bottom: 2rem;

    h3 {
        margin-top: 0;
        margin-bottom: 1.5rem;
        border-bottom: 1px solid #f1f5f9;
        padding-bottom: 0.5rem;
        color: #4f5358;
    }
  }

  .grid-2 {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1.5rem;
  }

  .input-field {
    display: flex;
    flex-direction: column;
    label {
        font-size: 0.8rem;
        font-weight: 600;
        margin-bottom: 0.5rem;
        color: #b1b1b1;
    }

    input {
      padding: 0.8rem;
      border-radius: 8px;
      border: 1px solid #bdbdbd;
      font-size: 1rem;
      color: #0f172a;
      &:focus { outline: none; border-color: #0f172a; }
    }
  }

  .empty-workspace {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    height: 100%;
    width: 100%;
    color: #00000056;
    font-size: 1.1rem;
    font-weight: 500;
    overflow-y: hidden;
  }

  .btn-save {
    background: rgb(59,130,246);
    color: #fff;
    border: none;
    padding: 0.8rem 1.5rem;
    border-radius: 8px;
    font-weight: 600;
    cursor: pointer;
    transition: opacity 0.2s;
    &:hover { opacity: 0.8; }
  }
}

.slide-fade-enter-active {
    transition: all 0.25s ease-out;
}

.slide-fade-leave-active {
    transition: all 0.15s ease-in;
}

.slide-fade-enter-from {
    opacity: 0; transform: translateY(10px);
}

.slide-fade-leave-to {
    opacity: 0; transform: translateY(-10px);
}
</style>