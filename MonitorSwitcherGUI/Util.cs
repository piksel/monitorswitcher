using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MonitorSwitcherGUI
{
    internal static class Util
    {
        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 10, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }

        public static DialogResult HotkeySetting(string title, string promptText, ref Hotkey value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();
            Button buttonClear = new Button();

            form.Text = title;
            label.Text = "Press hotkey combination or click 'Clear Hotkey' to remove the current hotkey";
            if (value != null)
                textBox.Text = value.ToString();
            textBox.Tag = value;

            buttonClear.Text = "Clear Hotkey";
            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 10, 372, 13);
            textBox.SetBounds(12, 36, 372 - 75 - 8, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);
            buttonClear.SetBounds(309, 36 - 1, 75, 23);

            buttonClear.Tag = textBox;

            void textBox_KeyUp(object sender, KeyEventArgs e)
            {
                if (textBox.Tag != null)
                {
                    Hotkey hotkey = (textBox.Tag as Hotkey);
                    // check if any additional key was pressed, if not don't acceppt hotkey
                    if ((hotkey.Key < Keys.D0) || ((!hotkey.Alt) && (!hotkey.Ctrl) && (!hotkey.Shift)))
                        textBox.Text = "";
                }
            }

            void textBox_KeyDown(object sender, KeyEventArgs e)
            {
                Hotkey hotkey = (textBox.Tag as Hotkey);
                if (hotkey == null)
                    hotkey = new Hotkey();
                hotkey.AssignFromKeyEventArgs(e);

                e.Handled = true;
                e.SuppressKeyPress = true; // don't add user input to text box, just use custom display

                textBox.Text = hotkey.ToString();
                textBox.Tag = hotkey; // store the current key combination in the textbox tag (for later use)
            }

            void buttonClear_Click(object sender, EventArgs e)
            {
                if (textBox.Tag != null)
                {
                    Hotkey hotkey = (textBox.Tag as Hotkey);
                    hotkey.RemoveKey = true;
                }
                textBox.Clear();
            }


            buttonClear.Click += new EventHandler(buttonClear_Click);
            textBox.KeyDown += new KeyEventHandler(textBox_KeyDown);
            textBox.KeyUp += new KeyEventHandler(textBox_KeyUp);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonClear.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel, buttonClear });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = (textBox.Tag as Hotkey);
            return dialogResult;
        }

       
    }


}
