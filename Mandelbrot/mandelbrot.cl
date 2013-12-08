#pragma OPENCL EXTENSION cl_khr_fp64: enable

double distanceSquared(double a, double b){
	return a * a + b * b;
}

__kernel void MandelBrotTest (int maxRecursion, double middleX, double middleY, double width, double height, int dataWidth, int dataHeight, __global int* result){   
	int id = get_global_id(0);

	int x = id % dataWidth;
	int y = id / dataWidth;
	
	double deltaX = width / dataWidth;
	double deltaY = height / dataHeight;
	
	double a = middleX - (width / 2.0) + (deltaX * x);
	double b = middleY - (height / 2.0) + (deltaY * y);

	double resultA = 0;
	double resultB = 0;
	result[id] = 0;

	for(int i = 1; i < maxRecursion; i++){
		double temp = resultA * resultA - resultB * resultB + a;
		resultB = 2 * resultA * resultB + b;
		resultA = temp;
		if(distanceSquared(resultA, resultB) > 4)
		{
			break;
		}
		result[id] = i - 1;
	}
}

__kernel void toBitmap(int maxRecursionCount, __global int* result, __global char* bitmap){
	int id = get_global_id(0);
	bitmap[id*4+0] = (char)(result[id] * 255 / maxRecursionCount);
	bitmap[id*4+1] = (char)(result[id] * 255 / maxRecursionCount);
	bitmap[id*4+2] = (char)(result[id] * 255 / maxRecursionCount);
	bitmap[id*4+3] = (char)(255);
}