import numpy as np
import matplotlib.pyplot as plt

areaLim = 10

def visualization(fileNum):
    positionsGivenNodes  = np.column_stack((np.char.replace(np.loadtxt("x{}.txt".format(fileNum),dtype=str), ',','.').astype(np.float32), np.char.replace(np.loadtxt("y{}.txt".format(fileNum),dtype=str), ',','.').astype(np.float32)))
    positionsOptNodes = np.column_stack((np.char.replace(np.loadtxt("xx.txt",dtype=str), ',','.').astype(np.float32) , np.char.replace(np.loadtxt("yy.txt",dtype=str), ',','.').astype(np.float32)))

    plt.plot(positionsGivenNodes[:,0],positionsGivenNodes[:,1], 'or', label='Given locations', linestyle = 'None')
    plt.plot(positionsOptNodes[:,0],positionsOptNodes[:,1], 'ob',   label='Optimized locations', linestyle = 'None')
    plt.xlim([-areaLim-2, areaLim+2])
    plt.ylim([-areaLim-2, areaLim+2])
    plt.plot([-areaLim, -areaLim, areaLim, areaLim, -areaLim], [-areaLim, areaLim, areaLim, -areaLim, -areaLim],'--k') 
    plt.title("Optimized points")
    plt.grid()
    plt.legend()
    plt.show()