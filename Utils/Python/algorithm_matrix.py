import numpy as np
import pandas as pd
import cv2
import sys
import json
from math import sqrt
import os

#
# As funções cgne_otimizada e calcular_cgnr não precisam de alteração.
#
def cgne_otimizada(H, g, tolerancia=1e-4, max_iter=10):
    f = np.zeros(H.shape[1], dtype=np.float64)
    r = g.copy()
    p = H.T @ r
    r_T_r = np.dot(r, r)
    
    print("\nIniciando o processo iterativo do CGNE (Otimizado)...", file=sys.stderr)
    print("-" * 60, file=sys.stderr)
    
    for i in range(max_iter):
        alpha = r_T_r / np.dot(p, p)
        f += alpha * p
        r -= alpha * (H @ p)
        r_T_r_novo = np.dot(r, r)
        erro = np.sqrt(r_T_r_novo)
        
        print(f"Iteração {i+1:03d}: Erro (Norma do Resíduo) = {erro:.6e}", file=sys.stderr)

        if erro < tolerancia:
            print("\nConvergência atingida!", file=sys.stderr)
            break
            
        beta = r_T_r_novo / r_T_r
        p = (H.T @ r) + beta * p
        r_T_r = r_T_r_novo
    else:
        print("\nO número máximo de iterações foi atingido.", file=sys.stderr)

    return f, i + 1, erro

def calcular_cgnr(H, g, tol=1e-4, max_iter=10):
    print("Iniciando o algoritmo CGNR...", file=sys.stderr)
    f = np.zeros(H.shape[1])
    r = g
    z = H.T @ r
    p = z
    norm_z_sq = np.dot(z, z)

    for i in range(max_iter):
        w = H @ p
        norm_w_sq = np.dot(w, w)
        alpha = norm_z_sq / norm_w_sq if norm_w_sq != 0 else 0
        
        f = f + alpha * p
        r = r - alpha * w
        z_next = H.T @ r
        norm_z_next_sq = np.dot(z_next, z_next)

        beta = norm_z_next_sq / norm_z_sq if norm_z_sq != 0 else 0
        
        p = z_next + beta * p
        norm_z_sq = norm_z_next_sq
        
        norm_r_atual = np.linalg.norm(r)
        if norm_r_atual < tol:
            print(f"\nConvergência atingida.", file=sys.stderr)
            break
    else:
        print(f"\nO algoritmo atingiu o número máximo de {max_iter} iterações.", file=sys.stderr)
        
    return f, i + 1, norm_r_atual

def gerar_e_salvar_imagem(f_vetor, altura, largura, nome_arquivo='resultado_imagem.png'):
    print(f"\nGerando a imagem do resultado...", file=sys.stderr)
    if f_vetor.size != altura * largura:
        raise ValueError(f"Dimensões incompatíveis. Vetor tem {f_vetor.size} elementos, imagem precisa de {altura*largura}.")

    imagem_normalizada = cv2.normalize(f_vetor, None, 0, 255, cv2.NORM_MINMAX, cv2.CV_8U)
    imagem_2d = imagem_normalizada.reshape((altura, largura))
    
    # Salva a imagem no disco.
    cv2.imwrite(nome_arquivo, imagem_2d)
    print(f"Imagem salva como '{nome_arquivo}'", file=sys.stderr)

    cv2.imshow('Imagem Resultante', imagem_final)
    cv2.waitKey(0)
    cv2.destroyAllWindows()
    

def main():
    try:
        data = json.load(sys.stdin)
    except json.JSONDecodeError:
        print(json.dumps({"status": "error", "message": "Erro: Não foi possível decodificar o JSON recebido."}))
        sys.exit(1)

    id_matriz = data['typeMatrix']
    g = np.array(data['signalV'])
    id_algoritmo = data['algorithm']
    tipSinal = data['typeSignal']
    
    if typeSignal == '3' || typeSignal == '6':
        max_iter = 1
    else:
        max_iter = 10 

    script_dir = os.path.dirname(__file__)
    caminho_matriz = os.path.join(script_dir, '..', 'Matrix', f'H-{id_matriz}.csv')

    try:
        H = pd.read_csv(caminho_matriz, header=None).values
    except FileNotFoundError:
        print(json.dumps({"status": "error", "message": f"Arquivo de matriz não encontrado em {caminho_matriz}"}))
        sys.exit(1)

    if id_algoritmo == 1:
        f_resultado, iteracoes, erro = calcular_cgnr(H, g, 1e-4, max_iter)
    else:
        f_resultado, iteracoes, erro = cgne_otimizada(H, g, 1e-4, max_iter)
    
    # Gera e salva a imagem do resultado
    tamanho_vetor = f_resultado.size
    dimensao = int(sqrt(tamanho_vetor))
    
    if dimensao * dimensao == tamanho_vetor:
        caminho_imagem_saida = "resultado_imagem.png"
        gerar_e_salvar_imagem(f_resultado, dimensao, dimensao, caminho_imagem_saida)
        
        resultado_final = {
            "status": "success",
            "imagePath": caminho_imagem_saida,
            "iterations": iteracoes,
            "finalError": erro
        }
    else:
        resultado_final = {
            "status": "warning",
            "message": f"Processamento concluído, mas a imagem não foi gerada (tamanho do vetor {tamanho_vetor} não é um quadrado perfeito)."
        }
    
    print(json.dumps(resultado_final))

if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        resultado_erro = {
            "status": "error",
            "message": str(e)
        }
        print(json.dumps(resultado_erro))
        sys.exit(1)