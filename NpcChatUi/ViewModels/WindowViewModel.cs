﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NpcChat.Util;
using NpcChat.ViewModels.Editors.Script;
using NpcChatSystem;
using NpcChatSystem.Data;
using NpcChatSystem.Data.Dialog;
using NpcChatSystem.Data.Dialog.DialogTreeItems;

namespace NpcChat.ViewModels
{
    class WindowViewModel : NotificationObject
    {
        public ObservableCollection<CharacterDialogModel> Speech
        {
            get => m_speech;
        }

        private NpcChatProject m_project;
        private ObservableCollection<CharacterDialogModel> m_speech = new ObservableCollection<CharacterDialogModel>();
        private DialogTree m_tree;

        public WindowViewModel()
        {
            m_project = new NpcChatProject();
            if (m_project.ProjectCharacters.RegisterNewCharacter(out int diane, new Character("diane")) &&
                m_project.ProjectCharacters.RegisterNewCharacter(out int jerry, new Character("jerry")))
            {
                DialogTree dialog = m_project.ProjectDialogs.CreateNewDialogTree();
                TreePart branch = dialog.CreateNewBranch();
                DialogSegment segment = branch.CreateNewDialog(diane);
                m_speech.Add(new CharacterDialogModel(m_project, segment));
                DialogSegment segment2 = branch.CreateNewDialog(jerry);
                m_speech.Add(new CharacterDialogModel(m_project, segment2));
            }
        }

        public void SetDialogTree(int dialogTreeId)
        {
            m_tree = NpcChatProject.Dialogs.GetDialog(dialogTreeId);
            TreePart part = m_tree.GetStart();

            List<CharacterDialogModel> tempList = new List<CharacterDialogModel>();
            foreach (DialogSegment segment in part.Dialog)
            {
                tempList.Add(new CharacterDialogModel(m_project, segment));
            }

            Speech.Clear();
            Speech.AddRange(tempList);
        }
    }
}
