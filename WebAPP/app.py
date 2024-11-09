from flask import Flask, request, jsonify
import pandas as pd
import numpy as np
from sqlalchemy import create_engine
import urllib
from sklearn.neighbors import NearestNeighbors
from flask_cors import CORS

app = Flask(__name__)
CORS(app)  # Cho phép tất cả các nguồn

app.config['DEBUG'] = True

# Kết nối cơ sở dữ liệu
params = urllib.parse.quote_plus(
    "DRIVER={ODBC Driver 17 for SQL Server};"
    "SERVER=AD\\MSSQLSERVER01;"
    "DATABASE=Shoping;"
    "Trusted_Connection=yes;"
)
engine = create_engine(f"mssql+pyodbc:///?odbc_connect={params}")

# Truy vấn dữ liệu
query = """
    SELECT o.UserId, od.ProductId, od.Quantity
    FROM Orders o
    JOIN OrderDetails od ON o.OrderCode = od.OrderCode
"""
data = pd.read_sql(query, engine)

data = data.groupby(['UserId', 'ProductId'], as_index=False).sum()

ratings_matrix = data.pivot(index='UserId', columns='ProductId', values='Quantity').fillna(0)

# Tạo mô hình Nearest Neighbors
model_knn = NearestNeighbors(metric='cosine', algorithm='brute')
model_knn.fit(ratings_matrix)

@app.route('/recommend', methods=['GET'])
def recommend():
    user_id = request.args.get('user_id')

    # Kiểm tra xem user_id có tồn tại trong ratings_matrix không
    if user_id not in ratings_matrix.index:
        return jsonify({'error': 'User ID not found'}), 404

    # Lấy hàng tương ứng với user_id
    user_row = ratings_matrix.loc[user_id].values.reshape(1, -1)

    # Tìm kiếm tất cả các sản phẩm gợi ý (sử dụng số hàng lớn hơn số lượng sản phẩm)
    distances, indices = model_knn.kneighbors(user_row, n_neighbors=ratings_matrix.shape[0])  # Lấy tất cả hàng

    # Lấy danh sách sản phẩm gợi ý
    recommended_product_indices = indices.flatten()  # Chuyển đổi thành 1D array
    recommended_products = ratings_matrix.columns[recommended_product_indices].tolist()

    return jsonify({'recommended_products': recommended_products})

if __name__ == '__main__':
    app.run(debug=True, port=5002)
