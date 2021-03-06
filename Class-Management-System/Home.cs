﻿using Class_Management_System.Entities;
using Class_Management_System.Forms;
using Class_Management_System.Global;
using Class_Management_System.Interfaces;
using Class_Management_System.Services;
using Class_Management_System.Structures;
using Class_Management_System.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Class_Management_System
{
    public partial class Home : Form
    {
        private readonly IGrafoService grafoService;
        private readonly IAulaService aulaService;

        private FormMateriasSemAula formMateriasSemAula;

        private HashSet<string> periodos;
        private HashSet<string> materias;
        private HashSet<string> professores;
        private HashSet<string> dias;
        private HashSet<string> horarios;
        private List<Aula> aulasSemHorario;

        private int reps;
        private bool expanded = false;
        private const int EXPAND_SIZE = 31;

        private const int EXPAND_SIZE_PERFIL = 26;
        private int reps_perfil;
        private bool expanded_perfil;
        public Home()
        {
            InitializeComponent();
            this.grafoService = DependencyFactory.Resolve<IGrafoService>();
            this.aulaService = DependencyFactory.Resolve<IAulaService>();

            try
            {
                this.periodos = new HashSet<string>();
                this.materias = new HashSet<string>();
                this.professores = new HashSet<string>();
                this.dias = new HashSet<string>();
                this.horarios = new HashSet<string>();
            }
            catch
            {
                DialogResult resultado = MessageBox.Show("Houve um problema na abertura do sistema.", "Config file",
                    MessageBoxButtons.OK, MessageBoxIcon.Question);
                this.Close();
            }
        }

        /// <summary>
        /// Limpa todos os componentes da tela relacionados ao grafo
        /// </summary>
        private void LimparValoresTela()
        {
            this.checkBoxSelecaoUnica.Checked = true;

            this.periodos.Clear();
            this.dias.Clear();
            this.horarios.Clear();
            this.professores.Clear();
            this.materias.Clear();

            this.cmbDiaSemana.Items.Clear();
            this.cmbHorario.Items.Clear();
            this.cmbMateria.Items.Clear();
            this.cmbPeriodo.Items.Clear();
            this.cmbProfessor.Items.Clear();
            this.dataGridGrafo.Rows.Clear();
        }

        /// <summary>
        /// Lê o arquivo com as informações das aulas, gera o grafo e exibe ele na tela
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGerarGrafo_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            if (openFile.ShowDialog() == DialogResult.OK)
            {
                FileInfo info = new FileInfo(openFile.FileName);
                txtFilePath.Text = info.FullName;

                try
                {
                    List<string> arquivo = LeitorArquivo.Ler(info.FullName);
                    this.LimparValoresTela();

                    List<Aula> aulas = this.aulaService.CriarListaDeAulas(arquivo);
                    this.lblTotalAulasArquivo.Text = "Total Aulas arquivo: " + this.CalcularTotalAulasArquivo(aulas);
                    List<IDado> dadosAula = new List<IDado>();

                    dadosAula.AddRange(aulas);
                    List<string> grafo = this.grafoService.GerarHorariosFormatados(Vertice.ConverterParaVertice(dadosAula), out this.aulasSemHorario);
                    this.groupFiltro.Enabled = true;
                    this.InserirResultadosNaTabela(grafo);
                    this.InserirListasNoComboBox();
                    this.PreencherValoresLabel(grafo.Count);
                }
                catch (Exception error)
                {
                    MessageBox.Show("Houve um problema na leitura do arquivo. Erro retornado: " + error.Message,
                        "Falha leitura arquivo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                if(this.aulasSemHorario.Count > 0)
                {
                    DialogResult resultado = MessageBox.Show("Existem aulas que não foram encaixadas em um horário. Deseja ve-las agora ?",
                        "Aulas", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if(resultado == DialogResult.Yes)
                    {
                        this.btnMateriasSemHorario.Visible = true;
                        this.formMateriasSemAula = new FormMateriasSemAula(this.aulasSemHorario);
                        this.formMateriasSemAula.ShowDialog();
                    }
                }
            }
        }

        private int CalcularTotalAulasArquivo(List<Aula> aulas)
        {
            int total = 0;
            aulas.ForEach(aula => total += aula.GetAulasPorSemana());
            return total;
        }

        private void PreencherValoresLabel(int totalResultados)
        {
            this.lblaulas_semana.Text = "Aulas semanais: " + totalResultados;
            this.lblProfessores.Text = "Professores: " + this.professores.Count;
            this.lblMaterias.Text = "Matérias: " + this.materias.Count;
        }

        /// <summary>
        /// Insere os valores de cada dado do grafo em uma lista HashSet
        /// </summary>
        /// <param name="periodo"></param>
        /// <param name="materia"></param>
        /// <param name="professor"></param>
        /// <param name="horario"></param>
        /// <param name="dia"></param>
        private void CarregarListaValores(string periodo, string materia, string professor, string horario, string dia)
        {
            this.periodos.Add(periodo);
            this.materias.Add(materia);
            this.professores.Add(professor);
            this.horarios.Add(horario);
            this.dias.Add(dia);
        }

        /// <summary>
        /// Insere os valores da lista de cada dado do grafo nos combobox respectivos à eles
        /// </summary>
        private void InserirListasNoComboBox()
        {
            foreach (string periodo in this.periodos) this.cmbPeriodo.Items.Add(periodo);
            foreach (string materia in this.materias) this.cmbMateria.Items.Add(materia);
            foreach (string professor in this.professores) this.cmbProfessor.Items.Add(professor);
            foreach (string horario in this.horarios) this.cmbHorario.Items.Add(horario);
            foreach (string dia in this.dias) this.cmbDiaSemana.Items.Add(dia);
        }

        /// <summary>
        /// Insere grafo no DataGridView
        /// </summary>
        /// <param name="resultados"></param>
        private void InserirResultadosNaTabela(List<string> resultados)
        {
            string[] divisao;

            if (resultados != null && resultados.Count > 0)
            {
                resultados.ForEach(resultado =>
                {
                    divisao = resultado.Split(';');
                    this.dataGridGrafo.Rows.Add(divisao[0], divisao[1], divisao[2], divisao[3], divisao[4]);
                    this.CarregarListaValores(divisao[0], divisao[1], divisao[2], divisao[3], divisao[4]);
                });
            }
        }

        /// <summary>
        /// Torna todas as linhas do DataGrid visíveis e desmarca todos os ComboBox
        /// </summary>
        private void HabilitarLinhasDataGrid()
        {
            foreach (DataGridViewRow linha in this.dataGridGrafo.Rows)
            {
                linha.Visible = true;
            }

            this.cmbDiaSemana.SelectedIndex = -1;
            this.cmbHorario.SelectedIndex = -1;
            this.cmbMateria.SelectedIndex = -1;
            this.cmbPeriodo.SelectedIndex = -1;
            this.cmbProfessor.SelectedIndex = -1;
        }

        /// <summary>
        /// Seleciona no DataGridView apenas os itens quem contém a informação selecionada no combox
        /// </summary>
        /// <param name="combo"></param>
        /// <param name="indexCell"></param>
        private void ExecutarFiltro(ComboBox combo, int indexCell)
        {
            if (combo.SelectedItem != null)
            {
                string itemSelecionado = combo.SelectedItem.ToString();
                if (this.checkBoxSelecaoUnica.Checked)
                {
                    foreach (DataGridViewRow linha in this.dataGridGrafo.Rows)
                    {
                        if (linha.Cells[indexCell].Value.ToString() != itemSelecionado) linha.Visible = false;
                        else linha.Visible = true;
                    }
                }
                else
                {
                    foreach (DataGridViewRow linha in this.dataGridGrafo.Rows)
                    {
                        if (linha.Cells[indexCell].Value.ToString() != itemSelecionado) linha.Visible = false;
                    }
                }
            }
        }

        /// <summary>
        /// Limpa os valores do ComboBox exceto aquele que foi selecionado
        /// </summary>
        /// <param name="combo"></param>
        private void LimparSelectedComboboxExceto(ComboBox combo)
        {
            if (combo.SelectedItem != null && this.checkBoxSelecaoUnica.Checked)
            {
                if (this.cmbDiaSemana.Name != combo.Name) this.cmbDiaSemana.SelectedIndex = -1;
                if (this.cmbHorario.Name != combo.Name) this.cmbHorario.SelectedIndex = -1;
                if (this.cmbMateria.Name != combo.Name) this.cmbMateria.SelectedIndex = -1;
                if (this.cmbPeriodo.Name != combo.Name) this.cmbPeriodo.SelectedIndex = -1;
                if (this.cmbProfessor.Name != combo.Name) this.cmbProfessor.SelectedIndex = -1;
            }
        }

        private void cmbPeriodo_SelectedValueChanged(object sender, EventArgs e)
        {
            this.ExecutarFiltro(this.cmbPeriodo, 0);
            this.LimparSelectedComboboxExceto(this.cmbPeriodo);
        }

        private void cmbMateria_SelectedValueChanged(object sender, EventArgs e)
        {
            this.ExecutarFiltro(this.cmbMateria, 1);
            this.LimparSelectedComboboxExceto(this.cmbMateria);
        }

        private void cmbProfessor_SelectedValueChanged(object sender, EventArgs e)
        {
            this.ExecutarFiltro(this.cmbProfessor, 2);
            this.LimparSelectedComboboxExceto(this.cmbProfessor);
        }

        private void cmbHorario_SelectedValueChanged(object sender, EventArgs e)
        {
            this.ExecutarFiltro(this.cmbHorario, 3);
            this.LimparSelectedComboboxExceto(this.cmbHorario);
        }

        private void cmbDiaSemana_SelectedValueChanged(object sender, EventArgs e)
        {
            this.ExecutarFiltro(this.cmbDiaSemana, 4);
            this.LimparSelectedComboboxExceto(this.cmbDiaSemana);
        }

        private void btnResetar_Click(object sender, EventArgs e)
        {
            this.HabilitarLinhasDataGrid();
        }

        private void DisplayTimer_Tick(object sender, EventArgs e)
        {
            if (this.expanded == false)
            {
                if (reps == 5)
                {
                    this.DisplayTimer.Enabled = false;
                    this.expanded = true;
                    this.reps = 0;
                }
                else
                {
                    reps++;
                    this.expanded = false;
                }
            }
            else
            {
                if (reps == 5)
                {
                    this.DisplayTimer.Enabled = false;
                    this.expanded = false;
                    this.reps = 0;
                }
                else
                {
                    reps++;
                }
            }
        }

        private void DisplayPerfil_Tick(object sender, EventArgs e)
        {
            if (this.expanded_perfil == false)
            {
                if (this.reps_perfil == 4)
                {
                    this.DisplayPerfil.Enabled = false;
                    this.expanded_perfil = true;
                    this.reps_perfil = 0;
                }
                else
                {
                    this.reps_perfil++;
                    this.expanded_perfil = false;
                }
            }
            else
            {
                if (this.reps_perfil == 4)
                {
                    this.DisplayPerfil.Enabled = false;
                    this.expanded_perfil = false;
                    this.reps_perfil = 0;
                }
                else
                {
                    this.reps_perfil++;
                }
            }
        }

        private void btnMateriasSemHorario_Click(object sender, EventArgs e)
        {
            this.formMateriasSemAula.ShowDialog();
        }
    }
}
